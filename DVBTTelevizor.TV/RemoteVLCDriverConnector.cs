using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;

namespace DVBTTelevizor.TV
{
    public class RemoteVLCDriverConnector : IDriverConnector
    {
        public int ConnectTimeoutSeconds { get; set; } = 5;

        private DVBTDriverConfiguration _driverConfiguration;
        private ILoggingService _log;
        private UDPStreamer _UDPStreamer;
        private long _lastTunedFreq = -1;
        private static object _readThreadLock = new object();
        private static object _infoLock = new object();
        private bool _readingStream = true;
        private bool _streaming = false;
        private bool _recording = false;
        private bool _readingBuffer = false;
        private string? _recordingFileName = null;
        private string _dataStreamInfo = "Data reading not initialized";
        private string _IP = "127.0.0.1";
        private int _port = 1234;
        private string _password = "1234";
        private readonly HttpClient _httpClient;
        private long _bitrate = 0;
        private bool _driverStreamDataAvailable = false;


        public event EventHandler? OnRawAudioDemodulated;
        public event EventHandler? OnServiceFound;
        public event EventHandler? RawDataReceived;
        public event EventHandler StatusChanged;


        public RemoteVLCDriverConnector(ILoggingService loggingService, string IP, int port, string password)
        {
            _log = loggingService;

            _IP = IP;
            _port = port;
            _password = password;

            _log.Debug($"Initializing remote VLC driver");

            _UDPStreamer = new UDPStreamer(_log);
            _driverConfiguration = new DVBTDriverConfiguration();

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://{_IP}:{_port}/"),
                Timeout = TimeSpan.FromSeconds(ConnectTimeoutSeconds)
            };

            // VLC vyžaduje prázdné uživatelské jméno a heslo u Basic Auth
            string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }


        public AppDriverTypeEnum DriverType
        {
            get
            {
                return AppDriverTypeEnum.DVBT;
            }
        }

        public DVBTDriverStateEnum State { get; private set; } = DVBTDriverStateEnum.Unknown;


        public bool Connected
        {
            get
            {
                return State.HasFlag(DVBTDriverStateEnum.Connected);
            }
        }

        public DriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DriverStreamTypeEnum.UDP;
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
                if (_UDPStreamer == null)
                {
                    return "udp://@localhost:1234";
                }

                return $"udp://@{_UDPStreamer.IP}:{_UDPStreamer.Port}";
            }
        }

        public int QueueSize
        {
            get
            {
                return 0;
            }
        }

        public bool Synced
        {
            get
            {
                return DriverStreamDataAvailable;
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

        public string PublicDirectory { get; set; } = "";

        public string DataStreamInfo
        {
            get
            {
                lock (_infoLock)
                {
                    return _dataStreamInfo;
                }
            }
            set
            {
                lock (_infoLock)
                {
                    _dataStreamInfo = value;
                }
            }
        }

        public long Bitrate
        {
            get
            {
                return _bitrate;
            }
        }

        public long LastTunedFreq
        {
            get
            {
                return _lastTunedFreq;
            }
        }

        public bool DriverStreamDataAvailable
        {
            get
            {
                return _driverStreamDataAvailable;
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

        public Task CheckPIDs()
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckStatus()
        {
            return Task.Run(async () =>
            {
                var state = await GetStatus();
                return state.SuccessFlag == true;
            });
        }

        public void Clear()
        {

        }

        public void Connect()
        {
            Connect(ConnectTimeoutSeconds);
        }

        public void Connect(int timeoutSeconds)
        {
            _log.Debug($"Connecting (timeout: {timeoutSeconds}s)");

            if (State == DVBTDriverStateEnum.Connected)
            {
                _log.Debug($"Already connected");
                //return;
            }

            State = DVBTDriverStateEnum.Connecting;

            // Run the (potentially blocking) TCP connect on a background thread
            // and enforce a timeout so we never get stuck in the Connecting state.
            Task.Run(async () =>
            {
                try
                {
                    var state = await GetStatus();

                    if (state == null || !state.SuccessFlag)
                    {
                        State = DVBTDriverStateEnum.Disconnected;
                    } else
                    {
                        State = DVBTDriverStateEnum.Connected;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error connecting remote VLC");
                    State = DVBTDriverStateEnum.Disconnected;
                }
            });
        }

        public Task Disconnect()
        {
            throw new NotImplementedException();
        }

        public Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            throw new NotImplementedException();
        }

        public async Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return new DVBTDriverCapabilities()
            {
                SuccessFlag = true,

                supportedDeliverySystems = 3,
                minFrequency = 474000000,
                maxFrequency = 714000000,
                frequencyStepSize = 8,
                vendorId = 0,
                productId = 0
            };
        }

        public async Task<string?> SendRequestAsync(string endpoint)
        {
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var resp = await response.Content.ReadAsStringAsync();
                _log.Debug(resp);
                return resp;
            }

            return null;
        }

        public async Task<DVBTDriverStatus> GetStatus()
        {
            var res = new DVBTDriverStatus();

            try
            {
                string? result = await SendRequestAsync("requests/status.xml");

                if (result != null)
                {
                    res.SuccessFlag = true;
                }

            }
            catch (Exception ex)
            {
                res.SuccessFlag = false;
            }

            return res;
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
            throw new NotImplementedException();
        }

        public Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverSearchPIDsResult> SearchProgramPIDs(long mapPID, bool setPIDsAndSync)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverSearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            throw new NotImplementedException();
        }

        public Task SetGain(GainEnum gain, int value = 0)
        {
            return Task.CompletedTask;
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
                    PIDs = new List<long>() { mapPID },
                    Result = DVBTDriverSearchProgramResultEnum.OK
                };
            });
        }

        public void StartRecording(string path)
        {
            throw new NotImplementedException();
        }

        public void StartStream()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Stop()
        {
            throw new NotImplementedException();
        }

        public string StopRecording()
        {
            throw new NotImplementedException();
        }

        public void StopStream()
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            throw new NotImplementedException();
        }

        public Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverTuneResult> WaitForSignal(bool fastTuning)
        {
            throw new NotImplementedException();
        }
    }
}
