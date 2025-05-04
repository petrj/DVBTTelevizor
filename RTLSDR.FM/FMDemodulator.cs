using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using LoggerService;
using RTLSDR.Common;

namespace RTLSDR.FM
{
    public class FMDemodulator : IDemodulator
    {
        public int Samplerate { get; set; } = 96000;
        public bool Emphasize { get; set; } = false;

        private object _lock = new object();
        private bool _finish = false;

        private int _bufferSize = 100 * 1024; // 10 kb demodulation buffer
        private byte[] _buffer = null;
        private short[] _demodBuffer = null;

        // https://github.com/osmocom/rtl-sdr/blob/master/src/rtl_fm.c

        private short pre_r = 0;
        private short pre_j = 0;
        private short now_r = 0;
        private short now_j = 0;
        private int prev_index = 0;

        public int BufferSize
        {
            get
            {
                return _bufferSize;
            }
            set
            {
                _bufferSize = value;
                ResizeBuffer();
            }
        }

        public double PercentSignalPower
        {
            get
            {
                return _powerPercent;
            }
        }

        private BackgroundWorker _worker = null;
        private ILoggingService _loggingService;

        private Queue<byte> _queue = new Queue<byte>();
        private DateTime _lastQueueSizeNotifyTime = DateTime.MinValue;
        private DateTime _lastPowerPercentNotifyTime = DateTime.MinValue;

        public event EventHandler OnDemodulated;
        public event EventHandler OnFinished;
        public event EventHandler OnServiceFound;
        public event EventHandler OnSpectrumDataUpdated = null;

        //public delegate void OnDemodulatedEventHandler(object sender, DataDemodulatedEventArgs e);
        //public delegate void OnFinishedEventHandler(object sender, EventArgs e);

        PowerCalculation _powerCalculator = new PowerCalculation();
        private double _powerPercent = 0;
        private double _audioBitrate = 0;

        public FMDemodulator(ILoggingService loggingService)
        {
            _loggingService = loggingService;

            ResizeBuffer();

            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += _worker_DoWork; ;
            _worker.RunWorkerAsync();
        }

        private void ResizeBuffer()
        {
            _buffer = new byte[_bufferSize];
            _demodBuffer = new short[_bufferSize];
        }

        public void Finish()
        {
            _finish = true;
        }

        public void Stop()
        {
            Finish();
            _worker.CancelAsync();
        }

        public double AudioBitrate
        {
            get
            {
                return _audioBitrate;
            }
        }

        private void _worker_DoWork(object sender, DoWorkEventArgs e)
        {
            _loggingService.Info($"Starting FM demodulator worker thread`");

            var bitRateCalculator = new BitRateCalculation(_loggingService, "FF audio");

            var processed = false;
            var processedBytesCount = 0;
            var bytesInQueue = 0;

            while (!_worker.CancellationPending)
            {
                processed = false;
                lock (_lock)
                {
                    bytesInQueue = _queue.Count;
                    if (bytesInQueue >= BufferSize || _finish)
                    {
                        processedBytesCount = BufferSize;
                        if (_finish && (bytesInQueue < BufferSize))
                        {
                            processedBytesCount = bytesInQueue;
                        }

                        for (var i = 0; i < processedBytesCount; i++)
                        {
                            _buffer[i] = _queue.Dequeue();
                        }

                        processed = true;
                    }
                }

                if ((DateTime.Now - _lastQueueSizeNotifyTime).TotalSeconds > 5)
                {
                    _loggingService.Info($"FM queue size: {(_queue.Count / 1024).ToString("N0")} KB");
                    _lastQueueSizeNotifyTime = DateTime.Now;
                }

                if (!processed)
                {
                    Thread.Sleep(50);
                }
                else
                {
                    if (OnDemodulated != null)
                    {
                        var arg = new DataDemodulatedEventArgs();

                        if (Emphasize)
                        {
                            var lowPassedDataMonoDeemphLength = LowPassWithMove(_buffer, _demodBuffer, processedBytesCount, 170000, -127);
                            var demodulatedDataMono2Length = FMDemodulate(_demodBuffer, lowPassedDataMonoDeemphLength, true);
                            DeemphFilter(_demodBuffer, demodulatedDataMono2Length, 170000);
                            var finalBytesCount = LowPassReal(_demodBuffer, demodulatedDataMono2Length, 170000, 32000);
                            arg.Data = GetBytes(_demodBuffer, finalBytesCount);
                            arg.AudioDescription = new AudioDataDescription()
                            {
                                BitsPerSample = 16,
                                Channels = 1,
                                SampleRate = 32000
                            };

                            _audioBitrate = bitRateCalculator.UpdateBitRate(finalBytesCount);
                        }
                        else
                        {
                            var lowPassedDataLength = LowPassWithMove(_buffer, _demodBuffer, processedBytesCount, Samplerate, -127);

                            if ((DateTime.Now - _lastPowerPercentNotifyTime).TotalSeconds > 5)
                            {
                                _powerPercent = _powerCalculator.GetPowerPercent(_demodBuffer, lowPassedDataLength);
                                _loggingService.Info($"FM power: {_powerPercent.ToString("N0")}%");
                                _lastPowerPercentNotifyTime = DateTime.Now;
                            }

                            var demodulatedDataMonoLength = FMDemodulate(_demodBuffer, lowPassedDataLength, false);

                            arg.Data = GetBytes(_demodBuffer, demodulatedDataMonoLength);
                            arg.AudioDescription = new AudioDataDescription()
                            {
                                BitsPerSample = 16,
                                Channels = 1,
                                SampleRate = 96000
                            };

                            _audioBitrate = bitRateCalculator.UpdateBitRate(demodulatedDataMonoLength);
                        }

                        OnDemodulated(this, arg);
                    }

                    if (_finish && OnFinished != null && bytesInQueue == 0)
                    {
                        OnFinished(this, new EventArgs());
                        _finish = false;
                    }
                }
            }

            _loggingService.Info($"FM demodulator worker thread finished`");
        }

        public int LowPassReal(short[] lp, int count, int sampleRateOut = 170000, int sampleRate2 = 32000)
        {
            int now_lpr = 0;
            int prev_lpr_index = 0;

            int i = 0;
            int i2 = 0;

            while (i < count)
            {
                now_lpr += lp[i];
                i++;
                prev_lpr_index += sampleRate2;

                if (prev_lpr_index < sampleRateOut)
                {
                    continue;
                }

                lp[i2] = Convert.ToInt16(now_lpr / ((double)sampleRateOut / (double)sampleRate2));

                prev_lpr_index -= sampleRateOut;
                now_lpr = 0;
                i2 += 1;
            }

            return i2;
        }

        public void DeemphFilter(short[] lp, int count, int sampleRate = 170000)
        {
            var deemph_a = Convert.ToInt32(1.0 / ((1.0 - Math.Exp(-1.0 / (sampleRate * 75e-6)))));

            var avg = 0;
            for (var i = 0; i < count; i++)
            {
                var d = lp[i] - avg;
                if (d > 0)
                {
                    avg += (d + deemph_a / 2) / deemph_a;
                }
                else
                {
                    avg += (d - deemph_a / 2) / deemph_a;
                }

                lp[i] = Convert.ToInt16(avg);
            }
        }

        private static byte[] GetBytes(short[] data, int count)
        {
            var res = new byte[count * 2];

            var pos = 0;
            for (int i = 0; i < count; i++)
            {
                var dataToWrite = BitConverter.GetBytes(data[i]);
                res[pos + 0] = (byte)dataToWrite[0];
                res[pos + 1] = (byte)dataToWrite[1];
                pos += 2;
            }

            return res;
        }

        public void AddSamples(byte[] IQData, int length)
        {
            // adding to queue
            lock (_lock)
            {
                for (var i = 0; i < length; i++)
                {
                    _queue.Enqueue(IQData[i]);
                }
            }
        }

        public static short PolarDiscriminant(int ar, int aj, int br, int bj)
        {
            double angle;

            // multiply
            var cr = ar * br - aj * (-bj);
            var cj = aj * br + ar * (-bj);

            angle = Math.Atan2(cj, cr);
            return (short)(angle / 3.14159 * (1 << 14));
        }

        private static int fast_atan2(int y, int x)
        /* pre scaled for int16 */
        {
            int yabs, angle;
            int pi4 = (1 << 12), pi34 = 3 * (1 << 12);  // note pi = 1<<14
            if (x == 0 && y == 0)
            {
                return 0;
            }
            yabs = y;
            if (yabs < 0)
            {
                yabs = -yabs;
            }
            if (x >= 0)
            {
                angle = pi4 - pi4 * (x - yabs) / (x + yabs);
            }
            else
            {
                angle = pi34 - pi4 * (x + yabs) / (yabs - x);
            }
            if (y < 0)
            {
                return -angle;
            }
            return angle;
        }

        private static short FastPolarDiscriminant(int ar, int aj, int br, int bj)
        {
            var cr = ar * br - aj * (-bj);
            var cj = aj * br + ar * (-bj);

            return Convert.ToInt16(fast_atan2(cj, cr));
        }

        public int FMDemodulate(short[] lp, int count, bool fast = false)
        {
            //var res = new short[lp.Length / 2];

            lp[0] = PolarDiscriminant(lp[0], lp[1], pre_r, pre_j);

            for (var i = 2; i < (count - 2); i += 2)
            {
                if (fast)
                {
                    lp[i / 2] = FastPolarDiscriminant(lp[i], lp[i + 1], lp[i - 2], lp[i - 1]);
                }
                else
                {
                    lp[i / 2] = PolarDiscriminant(lp[i], lp[i + 1], lp[i - 2], lp[i - 1]);
                }
            }
            pre_r = lp[lp.Length - 2];
            pre_j = lp[lp.Length - 1];

            return count / 2;
        }

        public int LowPassWithMove(byte[] iqData, short[] res, int count, double samplerate, short moveVector)
        {
            int downsample = Convert.ToInt32((1000000 / samplerate) + 1);
            int adjustedCount = count - 1;
            int i = 0, i2 = 0;
            short nr = now_r, nj = now_j;
            int pi = prev_index;

            while (i < adjustedCount)
            {
                nr += (short)(iqData[i] + moveVector);
                nj += (short)(iqData[i + 1] + moveVector);
                i += 2;
                pi++;

                if (pi >= downsample)
                {
                    res[i2++] = nr;
                    res[i2++] = nj;
                    pi = 0;
                    nr = 0;
                    nj = 0;
                }
            }

            now_r = nr;
            now_j = nj;
            prev_index = pi;

            return i2;
        }

        public int LowPass(short[] iqData, int count, double samplerate)
        {
            var downsample = Convert.ToInt32((1000000 / samplerate) + 1);

            var i = 0;
            var i2 = 0;
            while (i < count - 1)
            {
                now_r += iqData[i + 0];
                now_j += iqData[i + 1];
                i += 2;
                prev_index++;
                if (prev_index < downsample)
                {
                    continue;
                }
                iqData[i2] = now_r;
                iqData[i2 + 1] = now_j;
                prev_index = 0;
                now_r = 0;
                now_j = 0;
                i2 += 2;
            }

            return i2;
        }

        public int LowPassWithMoveParallel(byte[] iqData, short[] res, int count, double samplerate, short moveVector)
        {
            int downsample = Convert.ToInt32((1000000 / samplerate) + 1);
            int adjustedCount = count - 1;

            var finalCount = 0;

            Parallel.For(0, adjustedCount / (2 * downsample), (index) =>
            {
                int i = index * 2 * downsample;
                short now_j = 0;
                short now_r = 0;

                for (int j = 0; j < downsample; j++, i += 2)
                {
                    now_r += (short)(iqData[i] + moveVector);
                    now_j += (short)(iqData[i + 1] + moveVector);
                }

                res[index * 2] = now_r;
                res[index * 2 + 1] = now_j;

                finalCount = index * 2 + 1;
            });

            return finalCount;
        }

        public int LowPassWithMoveOpt(byte[] iqData, short[] res, int count, double samplerate, short moveVector)
        {
            int downsample = Convert.ToInt32((1000000 / samplerate) + 1);
            int adjustedCount = count - 1;
            int i = 0, i2 = 0;
            short nr = now_r, nj = now_j;
            int pi = prev_index;

            // Optimalizace: Předvýpočet limitu pro smyčku
            int loopLimit = adjustedCount - (adjustedCount % (2 * downsample));

            while (i < loopLimit)
            {
                for (int end = i + 2 * downsample; i < end; i += 2)
                {
                    nr += (short)(iqData[i] + moveVector);
                    nj += (short)(iqData[i + 1] + moveVector);
                }

                res[i2++] = nr;
                res[i2++] = nj;
                nr = 0;
                nj = 0;
            }

            // Zpracování zbývajících dat
            for (; i < adjustedCount; i += 2)
            {
                nr += (short)(iqData[i] + moveVector);
                nj += (short)(iqData[i + 1] + moveVector);
            }

            now_r = nr;
            now_j = nj;
            prev_index = pi;

            return i2;
        }

        public static short[] Move(byte[] iqData, int count, short vector)
        {
            var buff = new short[count];

            for (int i = 0; i < count; i++)
            {
                buff[i] = (short)(iqData[i] + vector);
            }

            return buff;
        }

        /*
       #region AI

       public static short[] ExtractStereoSignal(short[] IQ, int sampleRate)
       {
           float pilotToneFrequency = 19000f; // Frekvence pilotního tónu pro FM stereo
           int bufferSize = IQ.Length / 2;

           short[] stereoSignal = new short[bufferSize];
           float pilotPhase = 0f;
           float pilotPhaseIncrement = 2f * (float)Math.PI * pilotToneFrequency / sampleRate;

           for (int i = 0; i < bufferSize; i++)
           {
               // Výpočet fáze aktuálního vzorku
               float phase = (float)Math.Atan2(IQ[i*2+1], IQ[i*2+0]);

               // Generování inverzního pilotního tónu
               float inversePilotTone = (float)Math.Sin(pilotPhase);

               // Aktualizace fáze pilotního tónu
               pilotPhase += pilotPhaseIncrement;
               if (pilotPhase > 2 * Math.PI) pilotPhase -= 2 * (float)Math.PI;

               // Modulace vzorku inverzním pilotním tónem pro získání stereo rozdílového signálu
               stereoSignal[i] = Convert.ToInt16(phase * inversePilotTone);
           }

           // Filtrace a další zpracování stereo signálu může být potřebné zde

           return stereoSignal;
       }

       // Metoda pro stereo demodulaci
       public static short[] DemodulateStereo(short[] IQ, int sampleRate)
       {
           var demod = new FMDemodulator();

           var monoSignalLength = demod.FMDemodulate(IQ, false);
           var stereoSignal = ExtractStereoSignal(IQ, sampleRate);

           short[] result = new short[monoSignal.Length*2];

           for (int i = 0; i < monoSignal.Length; i++)
           {
               result[i*2+0] = Convert.ToInt16((monoSignal[i] + stereoSignal[i]) / 2); // L = (Mono + Stereo) / 2
               result[i*2+1] = Convert.ToInt16((monoSignal[i] - stereoSignal[i]) / 2); // R = (Mono - Stereo) / 2
           }

           return result;
       }

       public static short[] DemodulateStereoDeemph(short[] IQ, int inputSampleRate, int outputSampleRate)
       {
           var demod = new FMDemodulator();

           var monoSignal = demod.FMDemodulate(IQ, false);
           var stereoSignal = ExtractStereoSignal(IQ, inputSampleRate);

           var deemphDataMonoSignal = demod.DeemphFilter(monoSignal, inputSampleRate);
           var deemphDataMonoSignalFinal = demod.LowPassReal(deemphDataMonoSignal, inputSampleRate, outputSampleRate);

           var deemphDataStereoSignal = demod.DeemphFilter(stereoSignal, inputSampleRate);
           var deemphDataStereoSignalFinal = demod.LowPassReal(deemphDataStereoSignal, inputSampleRate, outputSampleRate);

           short[] result = new short[deemphDataMonoSignalFinal.Length * 2];

           for (int i = 0; i < deemphDataMonoSignalFinal.Length; i++)
           {
               result[i * 2 + 0] = Convert.ToInt16((deemphDataMonoSignalFinal[i] + deemphDataStereoSignalFinal[i]) / 2); // L = (Mono + Stereo) / 2
               result[i * 2 + 1] = Convert.ToInt16((deemphDataMonoSignalFinal[i] - deemphDataStereoSignalFinal[i]) / 2); // R = (Mono - Stereo) / 2
           }

           return result;
       }

       #endregion
       */
    }
}