using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public class RTLSDRFMDriverConnector : RTLSDRDriverConnector
    {
        private DateTime _lastStationTest = DateTime.MinValue;
        private Dictionary<long, bool> _stationOnFrequency = new Dictionary<long, bool>();

        public RTLSDRFMDriverConnector(ILoggingService loggingService, ISDR driver, IDemodulator demodulator, int startupFrequency)
            : base(loggingService,  driver, demodulator, startupFrequency)
        {
        }

        public override AppDriverTypeEnum DriverType => AppDriverTypeEnum.FM;

        public override Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency = 88000000,
                    maxFrequency = 108000000,
                    frequencyStepSize = 1000,
                    SuccessFlag = true
                };
            });
        }

        public override void Connect()
        {
            _stationOnFrequency.Clear();
            _driver.Settings.SDRSampleRate = AudioTools.FMSampleRate;
            base.Connect();
        }

        public override void OnDataDemodulated(object? sender, EventArgs e)
        {
            if (e is DataDemodulatedEventArgs de)
            {
                if ((DateTime.Now - _lastStationTest).TotalMilliseconds > 300)
                {
                    _lastStationTest = DateTime.Now;
                    var station = IsStationPresent(de.Data);
                    if (station && !_stationOnFrequency.ContainsKey(_driver.Frequency))
                    {
                        _stationOnFrequency.Add(_driver.Frequency, station);
                    }

                    _log.Debug($"Station: {station}");
                }

                base.OnDataDemodulated(sender, e);
            }
        }

        public override Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return Task.Run(() =>
            {
                if (_stationOnFrequency.ContainsKey(_driver.Frequency) && _stationOnFrequency[_driver.Frequency])
                {
                    var dict = new Dictionary<ServiceDescriptor, long>();
                    dict.Add(new ServiceDescriptor()
                    {
                        Free = true,
                        Length = 0,
                        ProgramNumber = _driver.Frequency,
                        ProviderName = "FM radio",
                        ServiceName = $"{(_driver.Frequency / 1000000.0).ToString("N1")} FM ",
                        ServisType = (byte)DVBTDriverServiceType.Radio

                    }, _driver.Frequency);

                    return new DVBTDriverSearchProgramMapPIDsResult()
                    {
                        Result = DVBTDriverSearchProgramResultEnum.OK,
                        ServiceDescriptors = dict
                    };
                }
                else
                {
                    return new DVBTDriverSearchProgramMapPIDsResult()
                    {
                        Result = DVBTDriverSearchProgramResultEnum.NoProgramFound
                    };
                }
            });
        }

        public static bool IsStationPresent(byte[] interleavedPcm16)
        {
            if (interleavedPcm16 == null || interleavedPcm16.Length < 4000)
                return false;

            int sampleCount = interleavedPcm16.Length / 4; // stereo 16-bit = 4 bytes/frame
            float prev = 0f;
            int zeroCrossings = 0;

            double sumRms = 0, sumRms2 = 0;
            double totalPower = 0;
            int window = 960; // ~10 ms @ 96 kHz
            int rmsSamples = 0;

            double[] rmsBuffer = new double[sampleCount / window + 1];
            int rmsIndex = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                short left = BitConverter.ToInt16(interleavedPcm16, i * 4);
                short right = BitConverter.ToInt16(interleavedPcm16, i * 4 + 2);
                float mono = (left + right) * 0.5f / short.MaxValue;

                // Zero crossing count
                if ((mono > 0 && prev <= 0) || (mono < 0 && prev >= 0))
                    zeroCrossings++;
                prev = mono;

                // Power accumulation
                double sq = mono * mono;
                sumRms += sq;
                rmsSamples++;
                totalPower += sq;

                if (rmsSamples >= window)
                {
                    double rms = Math.Sqrt(sumRms / rmsSamples);
                    rmsBuffer[rmsIndex++] = rms;
                    sumRms = 0;
                    rmsSamples = 0;
                }
            }

            // Compute variance of RMS values (dynamics)
            int n = rmsIndex;
            if (n < 2) return false;

            double mean = 0, var = 0;
            for (int i = 0; i < n; i++) mean += rmsBuffer[i];
            mean /= n;
            for (int i = 0; i < n; i++) var += (rmsBuffer[i] - mean) * (rmsBuffer[i] - mean);
            var /= n;

            // Average power of the signal
            double avgPower = totalPower / sampleCount;

            // Normalized zero-crossing rate
            double zcr = (double)zeroCrossings / sampleCount;

            // --- Heuristic thresholds (tune as needed) ---
            bool hasDynamics = var > 1e-5;     // real audio has changing RMS
            bool notTooNoisy = zcr < 0.15;     // noise crosses zero very often
            bool strongSignal = avgPower > 0.001; // reject weak stations or static

            return hasDynamics && notTooNoisy && strongSignal;
        }


    }
}
