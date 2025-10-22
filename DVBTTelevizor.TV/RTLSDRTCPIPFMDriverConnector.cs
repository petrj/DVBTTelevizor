using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.FM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public class RTLSDRTCPIPFMDriverConnector : IDriverConnector
    {
        private ILoggingService _log;
        private ISDR _driver = null;
        private IDemodulator _demodulator = null;

        private const int MinFMSignalPower = 80;

        UDPStreamer _UDPStreamer = null;

        public RTLSDRTCPIPFMDriverConnector(ILoggingService loggingService)
        {
            _log = loggingService;

            _log.Debug($"Initializing RTLSDR TCP-IP FM Driver Connector");

            //_UDPStreamer = new UDPStreamer(_log);
            _driverConfiguration = new DVBTDriverConfiguration();

            _driver = new RTLSDRDriver(_log);
            _driver.OnDataReceived += _driver_OnDataReceived;

            _demodulator = new FMDemodulator(_log);
            _demodulator.OnDemodulated += _demodulator_OnDemodulated;
        }

        private void _demodulator_OnDemodulated(object? sender, EventArgs e)
        {
            if (e is DataDemodulatedEventArgs de)
            {
                _UDPStreamer.SendByteArray(de.Data, de.Data.Length);
            }
        }

        private void _driver_OnDataReceived(object? sender, OnDataReceivedEventArgs e)
        {
            if (_demodulator != null && e.Size>0)
            {
                _demodulator.AddSamples(e.Data, e.Size);

                // save raw data for analysis
                //RecordData(e.Data, e.Size);
            }
        }

        private Stream _recordStream = null;

        private void RecordData(byte[] data, int size)
        {
            var fileName = Path.Combine("/storage/emulated/0/Android/media/net.petrjanousek.DVBTTelevizor.MAUI/", $"{(_driver.Frequency / 1000)}_kHz.raw");

            if (!File.Exists(fileName))
            {
                _recordStream = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write);
            }

            _recordStream.Write(data, 0, size);
            _recordStream.Flush();
        }

        public DVBTDriverStateEnum State { get; private set; } = DVBTDriverStateEnum.Unknown;

        private DVBTDriverConfiguration _driverConfiguration;
        private bool _driverStreamDataAvailable = false;
        private string? _recordingFileName = null;

        private bool _driverInstalled = false;

        private bool _readingStream = true;
        private bool _streaming = false;
        private bool _recording = false;

        private static object _readThreadLock = new object();

        public DVBTDriverConfiguration Configuration
        {
            get
            {
                return _driverConfiguration;
            }
            set
            {
                _driverConfiguration = value;
            }
        }

        public string RecordFileName
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _recordingFileName;
                }
            }
        }

        public bool DriverStreamDataAvailable
        {
            get
            {
                return _driverStreamDataAvailable;
            }
        }

        public bool Streaming
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _streaming;
                }
            }
        }

        public bool Connected
        {
            get
            {
                return
                        _driverInstalled &&
                        _driver != null &&
                        _driver.State == DriverStateEnum.Connected;
            }
        }

        public bool DriverInstalled
        {
            get
            {
                return _driverInstalled;
            }
            set
            {
                _driverInstalled = value;
            }
        }

        public DVBTDriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DVBTDriverStreamTypeEnum.Stream;
            }
        }

        public Stream VideoStream
        {
            get
            {
                return null;
            }
        }

        public string StreamUrl
        {
            get
            {
                return $"udp://@localhost:{(_driver == null ? "1234" : "8012")}";
            }
        }

        public bool Recording
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _recording;
                }
            }
        }

        public bool ReadingStream
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _readingStream;
                }
            }
        }

        public string PublicDirectory { get; set; }

        public string DataStreamInfo { get; set; }

        public long Bitrate
        {
            get
            {
                if (!Connected)
                    return 0;

                return _driver.RTLBitrate;
            }
        }

        public long LastTunedFreq { get; set; } = 104000000;

        public event EventHandler StatusChanged;

        public Task CheckPIDs()
        {
            return Task.Run(() => { return; });
        }

        public Task<bool> CheckStatus()
        {
            return Task.Run(() =>
            {
                if (!_driverInstalled ||
                   _driver == null ||
                    _driver.State != DriverStateEnum.Connected)
                {
                    return false;
                }

                return _driver.State == DriverStateEnum.Connected;
            });
        }

        public void Connect()
        {
            _log.Info($"RTL SDR driver: Connecting");

            try
            {
                _driver.Settings.Streamport = _driverConfiguration.TransferPort;
                _driver.Settings.Port = _driverConfiguration.ControlPort;

                _driver.Init(new DriverInitializationResult());
                _driver.Installed = true;

                _driver.SetFrequency(Convert.ToInt32(LastTunedFreq)); // must be set before init due to Test driver

                _driver.SetSampleRate(_driver.Settings.SDRSampleRate);
                _driver.SetDirectSampling(0);
                _driver.SetFrequencyCorrection(0);
                _driver.SetGainMode(false);

                DriverInstalled = true;
                State = DVBTDriverStateEnum.Connected;

                _UDPStreamer = new UDPStreamer(_log, "127.0.0.1", 8012);
            }
            catch (Exception ex)
            {
                State = DVBTDriverStateEnum.Disconnected;
            }
        }

        public Task Disconnect()
        {
            return Task.Run(() =>
            {
                _driver.Disconnect();
                State = DVBTDriverStateEnum.Disconnected;
            });
        }

        public Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            return Task.Run(() => { return Connected && _driver.RTLBitrate > 0; });
        }

        public Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency = 88000000,
                    maxFrequency = 108000000,
                    frequencyStepSize = 1000
                };
            });
        }

        public Task<DVBTDriverStatus> GetStatus()
        {
            return Task.Run(() =>
            {
                var state = new DVBTDriverStatus()
                {
                    SuccessFlag = true
                };

                switch (_driver.State)
                {
                    case DriverStateEnum.DisConnected:
                    case DriverStateEnum.NotInitialized:
                    case DriverStateEnum.Error:
                        state.hasCarrier = 0;
                        state.hasSync = 0;
                        state.hasSignal = 0;
                        state.hasLock = 0;
                        state.rfStrengthPercentage = 0;
                    break;
                    case DriverStateEnum.Connected:
                        state.hasSignal = 1;
                        state.hasCarrier = 0;
                        state.hasSync = 0;
                        state.hasLock = 0;
                        state.rfStrengthPercentage = Convert.ToInt64(_demodulator?.PercentSignalPower);
                        break;
                }

                if (StatusChanged != null)
                {
                    StatusChanged(this, new DVBTDriverStatusChangedEventArgs() { Status = state });
                }

                return state;
            });
        }

        public Task<DVBTDriverVersion> GetVersion()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverVersion()
                {
                    SuccessFlag = true,
                    Version = 1
                };
            });
        }

        public Task<EITScanResult> ScanEPG(int msTimeout = 2000)
        {
            return Task.Run(() => { return new EITScanResult(); });
        }

        public Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return Task.Run(() =>
            {
                if (_demodulator.PercentSignalPower >= MinFMSignalPower)
                {
                    var dict = new Dictionary<ServiceDescriptor, long>();
                    dict.Add(new ServiceDescriptor()
                    {
                        Free = true,
                        Length = 0,
                        ProgramNumber = _driver.Frequency,
                        ProviderName = "FM radio",
                        ServiceName = $"{(_driver.Frequency / 1000000.0).ToString("N1")} FM ",
                        ServisType = 0

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
                        Result = DVBTDriverSearchProgramResultEnum.NoSignal
                    };
                }
            });
        }


        public Task<DVBTDriverSearchPIDsResult> SearchProgramPIDs(long mapPID, bool setPIDsAndSync)
        {
            return Task.Run(() =>
            {
                return new DVBTDriverSearchPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK
                };
            });
        }

        public Task<DVBTDriverSearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            return Task.Run(() =>
            {
                return new DVBTDriverSearchAllPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK
                };
            });
        }

        public Task<DVBTDriverResponse> SetPIDs(List<long> PIDs)
        {
            return Task.Run(() => {
                return new DVBTDriverResponse()
                {
                    SuccessFlag = true
                };
            });
        }

        public Task<DVBTDriverSearchPIDsResult> SetupChannelPIDs(long mapPID, bool fastTuning)
        {
            return Task.Run(() => {
                return new DVBTDriverSearchPIDsResult()
                {
                    PIDs = new List<long>(),
                    Result = DVBTDriverSearchProgramResultEnum.OK
                };
            });
        }

        public Task StartRecording(string path)
        {
            return Task.Run(() => { return; });
        }

        public void StartStream()
        {
        }

        public Task<bool> Stop()
        {
            return Task.Run(() => { return true; });
        }

        public void StopRecording()
        {
        }

        public void StopStream()
        {
        }

        public Task<DVBTDriverResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            _log.Info($"RTL SDR driver: Tuning {frequency}");

            return Task.Run(() =>
            {
                var successFlag = false;

                try
                {
                    if (_driver.State == DriverStateEnum.Connected)
                    {
                        _driver.SetFrequency(Convert.ToInt32(frequency));
                        successFlag = true;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }

                return new DVBTDriverResponse()
                {
                    SuccessFlag = successFlag
                };
            });
        }

        public async Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            _log.Info($"RTLSDRTCPIPFMDriverConnector: TuneEnhanced freq {frequency / 1000} kHz");

            var status = await GetStatus();

            if (!status.SuccessFlag)
            {
                _log.Debug($"Getting status failed");
                return new DVBTDriverTuneResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.Error
                };
            }

            var tuneResult = await Tune(frequency, bandWidth, deliverySystem);
            if (!tuneResult.SuccessFlag)
            {
                _log.Debug($"Tune failed");
                return new DVBTDriverTuneResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.Error
                };
            }

            //_demodulator.ClearBuffer();

            for (var i = 0; i < 15; i++)
            {
                _log.Info($"Demodulator signal power: {_demodulator.PercentSignalPower}");
                await Task.Delay(100);
            }

            if (_demodulator.PercentSignalPower >= MinFMSignalPower)
            {
                await Task.Delay(2000);

                return new DVBTDriverTuneResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK,
                    SignalState = new DVBTDriverStatus()
                    {
                        hasCarrier = 1,
                        hasLock = 1,
                        hasSync = 1,
                        hasSignal = 1,
                        SuccessFlag = true,
                        rfStrengthPercentage = Convert.ToInt64(_demodulator.PercentSignalPower)
                    }
                };
            }

            return new DVBTDriverTuneResult()
            {
                Result = DVBTDriverSearchProgramResultEnum.NoSignal,
                SignalState = new DVBTDriverStatus()
                {
                    hasCarrier = 10,
                    hasLock = 0,
                    hasSync = 0,
                    hasSignal = 0,
                    SuccessFlag = true,
                    rfStrengthPercentage = Convert.ToInt64(_demodulator.PercentSignalPower)
                }
            };

        }

        public Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000)
        {
            return Task.Run(() => { return; });
        }

        public Task<DVBTDriverTuneResult> WaitForSignal(bool fastTuning)
        {
            return Task.Run( () => { return new DVBTDriverTuneResult();  } );
        }

        public int FrequencyMinKHz
        {
            get { return 88000; }
        }

        public int FrequencyMaxKHz
        {
            get { return 108000; }
        }

        public int BandwidthMinKHz
        {
            get { return 100; }
        }

        public int BandwidthMaxKHz
        {
            get { return 100; }
        }
    }
}
