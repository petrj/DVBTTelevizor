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
    public class RTLSDRTestDriverConnector : IDriverConnector
    {
        protected ILoggingService _log;
        private IDemodulator _demodulator = null;
        private DateTime _lastTimeForGettingStatus = DateTime.MinValue;
        private AppDriverTypeEnum _driverType = AppDriverTypeEnum.DAB;
        private CancellationTokenSource? _cts;

        public event EventHandler OnRawAudioDemodulated;

        public DVBTDriverStateEnum State { get; private set; } = DVBTDriverStateEnum.Unknown;

        private DVBTDriverConfiguration _driverConfiguration;

        public RTLSDRTestDriverConnector(ILoggingService loggingService, IDemodulator demodulator, AppDriverTypeEnum driverType)
        {
            _log = loggingService;

            _log.Debug($"Initializing RTLSDR Test Driver Connector");

            _driverConfiguration = new DVBTDriverConfiguration();

            _demodulator = demodulator;
            _demodulator.OnDemodulated += OnDataDemodulated;

            _driverType = driverType;

            State = DVBTDriverStateEnum.Unknown;
        }

        public virtual AppDriverTypeEnum DriverType
        {
            get { return _driverType; }
        }

        public async Task SetGain(GainEnum gain, int value = 0)
        {
            return;
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
            if (_demodulator != null && e.Size > 0)
            {
                _demodulator.AddSamples(e.Data, e.Size);

                if ((_demodulator is DABProcessor dab) && ((DateTime.UtcNow - _lastTimeForGettingStatus).TotalMilliseconds > 500))
                {
                    _log.Debug(dab.Stat(true));
                    _lastTimeForGettingStatus = DateTime.UtcNow;
                }

                // save raw data for analysis
                //RecordData(e.Data, e.Size);
            }
        }

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
                return string.Empty;
            }
        }

        public bool DriverStreamDataAvailable
        {
            get
            {
                return true;
            }
        }

        public bool Streaming
        {
            get
            {
                return true;
            }
        }

        public bool Connected
        {
            get
            {
                return State.HasFlag(DVBTDriverStateEnum.Connected);
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
                return $"udp://@localhost:1234";
            }
        }

        public bool Recording
        {
            get
            {
                return false;
            }
        }

        public bool ReadingStream
        {
            get
            {
                return true;
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

                return 1;
            }
        }

        public long LastTunedFreq { get; set; } = 104000000;

        public event EventHandler StatusChanged;

        public Task CheckPIDs()
        {
            return Task.CompletedTask;
        }

        public Task<bool> CheckStatus()
        {
            return Task.Run(() =>
            {
                return Connected;
            });
        }

        public virtual void Connect()
        {
            _log.Info($"RTL SDR Test driver: Connecting");

            State = DVBTDriverStateEnum.Connected;

            _= Task.Run(async () =>
             {
                 _cts = new CancellationTokenSource();
                 await ReadData(_cts.Token);
             });
        }

        private async Task ReadData(CancellationToken token)
        {
            // read data from driver and feed to demodulator
            if (DriverType != AppDriverTypeEnum.FM)
            {
                return;
            }

            var fName = Path.Join(PublicDirectory, "FM.raw");

            if (!File.Exists(fName))
            {
                throw new FileNotFoundException($"File {fName} not found");
            }

            // 🔧 सेट your desired bitrate here (bytes per second)
            // Example: 240k samples/sec * 2 bytes (I+Q, 8-bit each)
            int bytesPerSecond = 2114628;

            const int bufferSize = 16 * 1024; // 16 KB buffer
            byte[] buffer = new byte[bufferSize];

            using var fs = new FileStream(
                fName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                useAsync: true);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long totalBytesRead = 0;

            while (!token.IsCancellationRequested)
            {
                int bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length, token);

                if (bytesRead == 0)
                {
                    // EOF reached – stop (or loop if needed)
                    break;
                }

                totalBytesRead += bytesRead;

                _demodulator.AddSamples(buffer, bytesRead);

                // 🧠 Timing control (keeps correct bitrate)
                double expectedSeconds = (double)totalBytesRead / bytesPerSecond;
                double actualSeconds = stopwatch.Elapsed.TotalSeconds;

                if (expectedSeconds > actualSeconds)
                {
                    int delayMs = (int)((expectedSeconds - actualSeconds) * 1000);
                    if (delayMs > 0)
                    {
                        await Task.Delay(delayMs, token);
                    }
                }
            }

        }

        public Task Disconnect()
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }
            State = DVBTDriverStateEnum.Disconnected;

            return Task.CompletedTask;
        }

        public Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            return Task.Run(() => { return Connected; });
        }

        public virtual Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency =  _driverType == AppDriverTypeEnum.FM ? AudioTools.FMMinFreq : AudioTools.DABMinFreq,
                    maxFrequency = _driverType == AppDriverTypeEnum.FM ? AudioTools.FMMaxFreq: AudioTools.DABMaxFreq,
                    frequencyStepSize = 1000
                };
            });
        }

        public Task<DVBTDriverStatus> GetStatus()
        {
            return Task.Run(() =>
            {
                var state = new DVBTDriverStatus();

                if (!Connected)
                {
                    state.SuccessFlag = false;
                    state.hasCarrier = 0;
                    state.hasSync = 0;
                    state.hasSignal = 0;
                    state.hasLock = 0;
                    state.rfStrengthPercentage = 0;
                } else
                {
                    state.SuccessFlag = true;
                    state.hasSignal = 1;
                    state.hasCarrier = 0;
                    state.hasSync = 0;
                    state.hasLock = 0;
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
                return new DVBTDriverResponse()
                {
                    SuccessFlag = true
                };
            });
        }

        public async Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            _log.Info($"RTLSDRTestDriverConnector: TuneEnhanced freq {frequency / 1000} kHz");

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
