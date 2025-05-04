using LoggerService;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;

namespace RTLSDR
{
    public class RTLSRDTestDriver : ISDR
    {
        public DriverStateEnum State { get; private set; } = DriverStateEnum.NotInitialized;

        private ILoggingService _loggingService;
        private double _bitrate = 0;
        private string _inputDirectory = null;

        public RTLSRDTestDriver(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        private int _frequency = 104000000;

        public string DeviceName
        {
            get
            {
                return "Test driver";
            }
        }

        public DriverSettings Settings { get; private set; } = new DriverSettings();

        public bool? Installed { get; set; } = true;

        public int Frequency
        {
            get
            {
                return _frequency;
            }
        }

        public TunerTypeEnum TunerType
        {
            get
            {
                return TunerTypeEnum.RTLSDR_TUNER_UNKNOWN;
            }
        }

        public long RTLBitrate
        {
            get
            {
                return Convert.ToInt64(_bitrate);
            }
        }

        public double PowerPercent
        {
            get
            {
                return 100;
            }
        }

        public double Power
        {
            get
            {
                return 1;
            }
        }

        public event EventHandler<OnDataReceivedEventArgs> OnDataReceived;

        public void Disconnect()
        {
            _loggingService.Info($"Disconnecting driver");

            State = DriverStateEnum.DisConnected;
        }

        public void SetErrorState()
        {
            _loggingService.Info($"Setting manually error state");
            State = DriverStateEnum.Error;
        }

        private void ProcessInput()
        {
            new Thread(() =>
            {
                    _loggingService.Info($"RTLSRDTestDriver thread started");

                    var bitRateCalculator = new BitRateCalculation(_loggingService, "Test driver");

                    State = DriverStateEnum.Connected;

                    var lastBufferFillNotify = DateTime.MinValue;

                    var fName = Path.Combine(_inputDirectory, (Frequency <= 108000000 ? "FM.raw" : "DAB.raw"));
                    var bufferSize = Frequency <= 108000000 ? 125 * 1024 : 1024 * 1024;

                    var IQDataBuffer = new byte[bufferSize];

                    using (var inputFs = new FileStream(fName, FileMode.Open, FileAccess.Read))
                    {
                        _loggingService.Info($"Total bytes : {inputFs.Length}");
                        long totalBytesRead = 0;

                        while (inputFs.Position < inputFs.Length && State == DriverStateEnum.Connected)
                        {
                            var bytesRead = inputFs.Read(IQDataBuffer, 0, bufferSize);
                            totalBytesRead += bytesRead;

                            if (OnDataReceived != null)
                            {
                                //_powerPercent = Demodulator.PercentSignalPower;

                                OnDataReceived(this, new OnDataReceivedEventArgs()
                                {
                                    Data = IQDataBuffer,
                                    Size = bytesRead
                                });

                                _bitrate = bitRateCalculator.UpdateBitRate(bytesRead);
                            }

                            if ((DateTime.Now - lastBufferFillNotify).TotalMilliseconds > 1000)
                            {
                                lastBufferFillNotify = DateTime.Now;
                                if (inputFs.Length > 0)
                                {
                                    var percents = totalBytesRead / (inputFs.Length / 100);
                                    _loggingService.Debug($" Processing input file:                   {percents} %");
                                }
                            }

                            System.Threading.Thread.Sleep(16);
                        }
                    }

                    _bitrate = 0;                

            }).Start();
        }

        public void Init(DriverInitializationResult driverInitializationResult)
        {
            _inputDirectory = driverInitializationResult.OutputRecordingDirectory;

            if (string.IsNullOrEmpty(_inputDirectory))
            {
                throw new Exception("No input directory");
            }
    
            //ProcessInput();            
        }

        public void SendCommand(Command command)
        {

        }

        public void SetAGCMode(bool automatic)
        {

        }

        public void SetDirectSampling(int value)
        {

        }

        public void SetFrequency(int freq)
        {
            _frequency = freq;

            ProcessInput();
        }

        public void SetFrequencyCorrection(int correction)
        {

        }

        public void SetGain(int gain)
        {

        }

        public void SetGainMode(bool manual)
        {

        }

        public void SetIfGain(bool ifGain)
        {

        }

        public void SetSampleRate(int sampleRate)
        {

        }
    }
}
