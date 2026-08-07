using DVBTTelevizor.MAUI;
using LoggerService;
using MPEGTS;
using Newtonsoft.Json.Bson;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.DAB;
using RTLSDR.FM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace DVBTTelevizor.TV
{
    public abstract class RTLSDRDriverConnector : IDriverConnector
    {
        protected ILoggingService _log;
        protected ISDR _driver = null;
        protected IDemodulator _demodulator = null;

        protected SpectrumWorker? _spectrumWorker;

        public event EventHandler OnRawAudioDemodulated;

        public long LastTunedFreq { get; set; }

        public event EventHandler StatusChanged;
        public event EventHandler? OnServiceFound;
        public event EventHandler? RawDataReceived;


        public const int SpectrumFFTSize = 16384;
        public const int SpectrumWidth = 1024;
        public const int SpectrumHeight = 100;
        public const int SpectrumHThresholdOffset = 15;
        public bool LastFreqHasSignal { get; set; } = false;
        public byte HasCarrier { get; set; } = 0;

        public byte HasSignal { get; set; } = 0;
        public byte HasLock { get; set; } = 0;
        public float RFStrengthPercentage { get; set; } = 0;

        private bool _connecting = false;
        private bool _disconnecting = false;

        public RTLSDRDriverConnector(ILoggingService loggingService, ISDR driver, IDemodulator demodulator, int startupFrequency)
        {
            LastTunedFreq = startupFrequency;
            _log = loggingService;

            _log.Debug($"Initializing RTLSDR TCP-IP Driver Connector");

            //_UDPStreamer = new UDPStreamer(_log);
            _driverConfiguration = new DVBTDriverConfiguration();

            _driver = driver;
            _driver.OnDataReceived += OnDataReceived;

            _demodulator = demodulator;
            _demodulator.OnDemodulated += OnDataDemodulated;
            _demodulator.OnServiceFound += Demodulator_OnServiceFound;

            _spectrumWorker = new SpectrumWorker(_log, SpectrumFFTSize, AudioTools.DABSampleRate);
        }

        protected void Demodulator_OnServiceFound(object? sender, EventArgs e)
        {
            if ((e is DABServiceFoundEventArgs de) && (de.Service != null))
            {
                _log.Info($"DAB service found: {de.Service}");

                OnServiceFound?.Invoke(this, e);
            }

            if (e is FMServiceFoundEventArgs)
            {
                _log.Info($"FM service found");

                OnServiceFound?.Invoke(this, e);
            }
        }

        public int QueueSize
        {
            get
            {
                return _demodulator == null ? 0 : _demodulator.QueueSize;
            }
        }

        public bool Synced
        {
            get
            {
                return _demodulator == null ? false : _demodulator.Synced;
            }
        }

        public virtual AppDriverTypeEnum DriverType
        {
            get { return AppDriverTypeEnum.DAB; }
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

        private void OnDataReceived(object? sender, OnDataReceivedEventArgs e)
        {
            _connecting = false;

            if (_demodulator != null && e.Size>0)
            {
                _demodulator.AddSamples(e.Data, e.Size);

                _spectrumWorker?.AddData(e.Data, e.Size);

                if (RawDataReceived != null)
                {
                    RawDataReceived(this, new RawDataReceivedEventArgs()
                    {
                        Data = e.Data,
                        DataSize = e.Size
                    });
                }

                // save raw data for analysis
                //RecordData(e.Data, e.Size);
            }
        }

        private Stream _recordStream = null;

        private DVBTDriverConfiguration _driverConfiguration;

        private bool _readingStream = true;
        private bool _streaming = false;
        private bool _recording = false;

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

        public virtual string RecordFileName
        {
            get
            {
                return String.Empty;
            }
        }

        public bool DriverStreamDataAvailable
        {
            get
            {
                return Connected && _driver.RTLBitrate > 0;
            }
        }

        public bool Streaming
        {
            get
            {
                return _streaming;
            }
        }

        public bool Connected
        {
            get
            {
                if (_driver == null)
                {
                    return false;
                }

                return _driver.State == DriverStateEnum.Connected;
            }
        }

        public DVBTDriverStateEnum State
        {
            get
            {
                if (_driver == null)
                {
                    return DVBTDriverStateEnum.Disconnected;
                }

                var res = DVBTDriverStateEnum.Unknown;

                switch (_driver.State)
                {
                    case DriverStateEnum.NotInitialized:
                    case DriverStateEnum.DisConnected:
                        if (_connecting)
                        {
                            res = DVBTDriverStateEnum.Connecting;
                        }
                        else
                        {
                            res = DVBTDriverStateEnum.Disconnected;
                        }
                        break;
                    case DriverStateEnum.Connected:
                        if (_disconnecting)
                        {
                            res = DVBTDriverStateEnum.DisConnecting;
                        }
                        else
                        {
                            res = DVBTDriverStateEnum.Connected;
                            res |= DVBTDriverStateEnum.Playing; // RTLSDR is always playing
                            if (Recording)
                            {
                                res |= DVBTDriverStateEnum.Recording;
                            }
                        }
                        break;
                    default:
                        res = DVBTDriverStateEnum.Unknown;
                        break;
                }

                return res;
            }
        }

        public virtual DriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DriverStreamTypeEnum.None;
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
                return _recording;
            }
        }

        public bool ReadingStream
        {
            get
            {
                return _readingStream;
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



        public Task CheckPIDs()
        {
            return Task.Run(() => { return; });
        }

        public Task<bool> CheckStatus()
        {
            return Task.Run(() =>
            {
                if (_driver == null ||
                    _driver.State != DriverStateEnum.Connected)
                {
                    return false;
                }

                _connecting = false;
                return _driver.State == DriverStateEnum.Connected;
            });
        }

        public virtual void Connect()
        {
            _log.Info($"RTL SDR driver: Connecting");

            _connecting = true;
            _disconnecting = false;

            Task.Run(() =>
            {
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

                    if (_spectrumWorker != null)
                    {
                        _spectrumWorker.Stop();
                    }
                    _spectrumWorker = new SpectrumWorker(_log, SpectrumFFTSize, _driver.Settings.SDRSampleRate);
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            });
        }

        public Task Disconnect()
        {
            _connecting = false;
            _disconnecting = true;

            return Task.Run(() =>
            {
                _driver.Disconnect();
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
                    SuccessFlag = true,
                    hasSignal = HasSignal,
                    hasCarrier = HasCarrier,
                    hasSync = (_demodulator != null && _demodulator.Synced) ? (byte)1 : (byte)0,
                    hasLock = HasLock,
                    rfStrengthPercentage = Convert.ToInt64(RFStrengthPercentage)
                };

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
                    Version = 2
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

        public virtual Task<DVBTDriverSearchPIDsResult> SetupChannelPIDs(long mapPID, bool fastTuning)
        {
            return Task.Run(() => {
                return new DVBTDriverSearchPIDsResult()
                {
                    PIDs = new List<long>() { mapPID },
                    Result = DVBTDriverSearchProgramResultEnum.OK
                };
            });
        }

        public virtual void StartRecording(string path)
        {
            _recording = true;
        }

        public virtual string StopRecording()
        {
            _recording = false;
            return string.Empty;
        }

        public void StartStream()
        {
        }

        public Task<bool> Stop()
        {
            return Task.Run(() => { return true; });
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
                        LastTunedFreq = frequency;
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

        public virtual bool IsOnSpectrumSignal()
        {
            return false;
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

            //_demodulator?.Clear();
            var tuneResult = await Tune(frequency, bandWidth, deliverySystem);

            if (!tuneResult.SuccessFlag)
            {
                _log.Debug($"Tune failed");
                return new DVBTDriverTuneResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.Error
                };
            }

            await Task.Delay(1000);

            var res = DVBTDriverSearchProgramResultEnum.NoSignal;

            HasCarrier = 0;
            HasLock = 0;
            HasSignal = 0;
            RFStrengthPercentage = 1;

            LastFreqHasSignal = false;

            for (int at = 0; at < 10; at++)
            {
                if (IsOnSpectrumSignal())
                {
                    LastFreqHasSignal = true;

                    HasCarrier = 1;
                    HasLock = 1;
                    HasSignal = 1;
                    RFStrengthPercentage = 100;

                    res = DVBTDriverSearchProgramResultEnum.OK;

                    break;
                }

                await Task.Delay(250);
            }

            return new DVBTDriverTuneResult()
            {
                Result = res,
                SignalState = new DVBTDriverStatus()
                {
                    hasCarrier = HasCarrier,
                    hasLock = HasLock,
                    hasSync = (_demodulator != null && _demodulator.Synced) ? (byte)1 : (byte)0,
                    hasSignal = HasSignal,
                    SuccessFlag = true,
                    rfStrengthPercentage = Convert.ToInt64(RFStrengthPercentage)
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

        public virtual void Clear()
        {
            _demodulator.Clear();
        }
    }
}
