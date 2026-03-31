using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.DAB;
using RTLSDR.FM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public abstract class RTLSDRDriverConnector : IDriverConnector
    {
        protected ILoggingService _log;
        protected ISDR _driver = null;
        private IDemodulator _demodulator = null;
        private DateTime _lastTimeForGettingStatus = DateTime.MinValue;

        public event EventHandler OnRawAudioDemodulated;

        public RTLSDRDriverConnector(ILoggingService loggingService, ISDR driver, IDemodulator demodulator)
        {
            _log = loggingService;

            _log.Debug($"Initializing RTLSDR TCP-IP Driver Connector");

            //_UDPStreamer = new UDPStreamer(_log);
            _driverConfiguration = new DVBTDriverConfiguration();

            _driver = driver;
            _driver.OnDataReceived += _driver_OnDataReceived;

            _demodulator = demodulator;
            _demodulator.OnDemodulated += OnDataDemodulated;
        }

        public async Task SetGain(GainEnum gain, int value = 0)
        {
            if (_driver == null)
            {
                return;
            }

            if (gain == GainEnum.HW)
            {
                _driver?.SetGain(0);
                _driver?.SetGainMode(false);
                _driver?.SetIfGain(true);
                _driver?.SetAGCMode(true);
            }
            else
            {
                // always manual
                _driver?.SetGainMode(true);
                if (gain == GainEnum.Auto)
                {
                    _driver?.SetGain(0);
                    await _driver?.AutoSetGain();
                }
                else
                {
                    _driver?.SetGain(value);
                }
            }
        }

        public virtual void OnDataDemodulated(object? sender, EventArgs e)
        {
            if (OnRawAudioDemodulated != null)
            {
                OnRawAudioDemodulated(sender, e);
            }
        }

        private void _driver_OnDataReceived(object? sender, OnDataReceivedEventArgs e)
        {
            if (_demodulator != null && e.Size>0)
            {
                _demodulator.AddSamples(e.Data, e.Size);

                if ((_demodulator is DABProcessor dab) && ((DateTime.UtcNow - _lastTimeForGettingStatus).TotalMilliseconds>500))
                {
                    _log.Debug(dab.Stat(true));
                    _lastTimeForGettingStatus = DateTime.UtcNow;
                }

                // save raw data for analysis
                //RecordData(e.Data, e.Size);
            }
        }

        private Stream _recordStream = null;

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
                return DVBTDriverStreamTypeEnum.None;
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

        public virtual void Connect()
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

        public virtual Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency =  88000000,
                    maxFrequency = 852000000,
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
                        //state.rfStrengthPercentage = Convert.ToInt64(_demodulator?.per);
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

        public virtual Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return Task.Run(() =>
            {
                return new DVBTDriverSearchProgramMapPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.NoProgramFound
                };
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
                    //rfStrengthPercentage = Convert.ToInt64(_demodulator.PercentSignalPower)
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
    }
}
