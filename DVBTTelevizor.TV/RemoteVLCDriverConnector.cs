using DVBTTelevizor.MAUI;
using LoggerService;
using MPEGTS;
using NLog.Targets;
using RTLSDR;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public class RemoteVLCDriverConnector : IDriverConnector
    {
        public int ConnectTimeoutSeconds { get; set; } = 5;
        public int ReceiveTimeoutMiliSeconds { get; set; } = 5000;
        public int ReadBufferSize { get; set; } = 32768;

        private DVBTDriverConfiguration _driverConfiguration;
        private ILoggingService _log;
        private UDPStreamer _UDPStreamer;
        private long _lastTunedFreq = -1;
        private int _lastTunedDeliverySystem = -1;
        private long _lastTunedBandwidth = -1;
        private static object _readThreadLock = new object();
        private static object _infoLock = new object();
        private bool _readingStream = true;
        private bool _streaming = false;
        private bool _recording = false;
        private bool _readingBuffer = false;
        private string? _recordingFileName = null;
        private string _dataStreamInfo = "Data reading not initialized";
        private string _IP = "127.0.0.1";
        private int _port = 8080;
        private string _password = "1234";

        List<byte> _readBuffer = new List<byte>();

        // VLC http communication
        private readonly HttpClient _httpClient;

        private long _bitrate = 0;
        private bool _driverStreamDataAvailable = false;
        private string _recordDirectory = "";

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

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            _log.Debug("Starting DVBT reader thread");

            var totalBytesRead = 0;
            _bitrate = 0;
            string? _lastSpeedCalculationSec = null;

            try
            {
                DataStreamInfo = "";

                FileStream recordFileStream = null;
                long bytesReadFromLastMeasureStartTime = 0;

                bool readingStream = true;
                bool rec = false;
                bool readingBuffer = false;
                bool streaming = false;

                DateTime lastBitRateMeasureStartTime = DateTime.Now;

                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                UdpClient udpClient = new UdpClient(1234);

                do
                {
                    lock (_readThreadLock)
                    {
                        // sync reading record state
                        rec = _recording;
                        readingBuffer = _readingBuffer;
                        readingStream = _readingStream;
                        streaming = _streaming;
                    }

                    string status = String.Empty;

                    if (_lastTunedFreq >= 0)
                    {
                        status = $"Tuned {(_lastTunedFreq / 1000000).ToString("N2")} MHz";
                    }
                    else
                    {
                        status = $"Not tuned";
                    }

                    if (!readingStream)
                    {
                        status += ", not reading";
                        System.Threading.Thread.Sleep(50);
                    }
                    else
                    {
                        status += ", reading";

                        if (rec)
                        {
                            status += ", recording";
                        }
                        if (readingBuffer)
                        {
                            status += ", bufferring";
                        }
                        if (streaming)
                        {
                            status += ", streaming";
                        }

                        if (udpClient.Available > 0)
                        {
                            var buffer = udpClient.Receive(ref remoteEndPoint);
                            var bytesRead = buffer.Length;
                            totalBytesRead += bytesRead;
                            bytesReadFromLastMeasureStartTime += bytesRead;

                            if (RawDataReceived != null)
                            {
                                RawDataReceived(this, new RawDataReceivedEventArgs()
                                {
                                    Data = buffer,
                                    DataSize = bytesRead
                                });
                            }

                            if (rec)
                            {
                                if (recordFileStream == null)
                                {
                                    var fileNameFreq = (_lastTunedFreq / 1000000).ToString() + "MHz";
                                    _recordingFileName = Path.Combine(_recordDirectory, $"DVBT-MPEGTS-{fileNameFreq}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.ts");

                                    if (System.IO.File.Exists(_recordingFileName))
                                        System.IO.File.Delete(_recordingFileName);

                                    recordFileStream = new FileStream(_recordingFileName, FileMode.Create, FileAccess.Write);
                                }

                                recordFileStream.Write(buffer, 0, bytesRead);
                            }
                            if (readingBuffer)
                            {
                                lock (_readThreadLock)
                                {
                                    if (_readingBuffer)
                                    {
                                        for (var i = 0; i < bytesRead; i++)
                                            _readBuffer.Add(buffer[i]);
                                    }
                                }
                            }
                            if (streaming)
                            {
                                _UDPStreamer.SendByteArray(buffer, bytesRead);

                                if (!_driverStreamDataAvailable && bytesRead > 0)
                                {
                                    _driverStreamDataAvailable = true;
                                    _log.Debug("DVBT driver data available");
                                }
                            }

                        }
                        else
                        {
                            System.Threading.Thread.Sleep(50);
                        }

                        if (!rec && recordFileStream != null)
                        {
                            recordFileStream.Flush();
                            recordFileStream.Close();
                            recordFileStream = null;
                        }

                        // calculating speed

                        var currentLastSpeedCalculationSec = DateTime.Now.ToString("yyyyMMddhhmmss");

                        if (_lastSpeedCalculationSec != currentLastSpeedCalculationSec)
                        {
                            // occurs once per second

                            if (bytesReadFromLastMeasureStartTime > 0)
                            {
                                _bitrate = bytesReadFromLastMeasureStartTime * 8;

                                status += $"({DVBTDriverConnector.GetHumanReadableBitRate(_bitrate)})";
                            }

                            //_log.Debug($"{status}");

                            bytesReadFromLastMeasureStartTime = 0;
                            _lastSpeedCalculationSec = currentLastSpeedCalculationSec;
                        }
                    }

                    DataStreamInfo = status;

                }
                while (_readingStream);

            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while reading from TransferPort");
            }

            _log.Debug($"Reading data finished");
            DataStreamInfo = "Reading data finished";
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
                    return "udp://@:1234";
                }

                return $"udp://@:{_UDPStreamer.Port}";
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

        private void StartReadBuffer()
        {
            lock (_readThreadLock)
            {
                _log.Debug($"starting read buffer");

                _readBuffer.Clear();
                _readingBuffer = true;
            }
        }

        private byte[] GetReadBufferData()
        {
            lock (_readThreadLock)
            {
                if (_readBuffer.Count == 0)
                    return null;

                return _readBuffer.ToArray();
            }
        }

        private bool BufferContainsData()
        {
            lock (_readThreadLock)
            {
                //_log.Debug($"Getting buffer count");

                return _readBuffer.Count > 0;
            }
        }

        private void ClearReadBuffer()
        {
            lock (_readThreadLock)
            {
                _log.Debug($"Clearing buffer");

                _readBuffer.Clear();
            }
        }

        private void StopReadBuffer()
        {
            lock (_readThreadLock)
            {
                _log.Debug($"Stopping read buffer (total bytes found: {_readBuffer.Count})");

                _readingBuffer = false;
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

        private async Task StartBackgroundReadingAsync(int timeoutSeconds)
        {
            _log.Debug($"Starting background reading (timeout: {timeoutSeconds}s)");

            var recordBackgroundWorker = new BackgroundWorker();
            recordBackgroundWorker.DoWork += worker_DoWork;
            recordBackgroundWorker.RunWorkerAsync();

            State = DVBTDriverStateEnum.Connected;
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

                        await StartBackgroundReadingAsync(timeoutSeconds).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error connecting remote VLC");
                    State = DVBTDriverStateEnum.Disconnected;
                }
            });
        }

        private string GetLocalIPAddressForRemote(string remoteIp)
        {
            try
            {
                if (remoteIp == "127.0.0.1" || remoteIp.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    return "127.0.0.1";
                }

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect(remoteIp, 65530);
                    if (socket.LocalEndPoint is IPEndPoint endPoint)
                    {
                        return endPoint.Address.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "RemoteVLCDriverConnector: Failed to resolve local IP for remote VLC");
            }

            return "127.0.0.1";
        }
        public Task Disconnect()
        {
            return Task.Run(async () =>
            {
                await Stop();
                State = DVBTDriverStateEnum.Disconnected;
            });
        }

        public Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            return Task.FromResult(_driverStreamDataAvailable);
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
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                var resp = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _log.Debug($"VLC Response [{endpoint}]: {resp}");
                    return resp;
                }

                _log.Error($"VLC HTTP error [{(int)response.StatusCode} {response.ReasonPhrase}] for {endpoint}: {resp}");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"VLC Request failed for {endpoint}");
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
                _log.Error(ex, "RemoteVLCDriverConnector: GetStatus error");
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
            return Task.FromResult(new EITScanResult());
        }

        public async Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            var startTime = DateTime.Now;
            var timeoutForReadingBuffer = 15; //  15 secs

            while ((DateTime.Now - startTime).TotalSeconds < timeoutForReadingBuffer)
            {
                try
                {
                  var jsonResponse = await SendRequestAsync("requests/status.json");
                  if (!string.IsNullOrWhiteSpace(jsonResponse))
                  {
                    var status = JsonSerializer.Deserialize<VlcStatusResponse>(jsonResponse);
                    var serviceDescriptors = new Dictionary<ServiceDescriptor, long>();

                    if (status?.Information?.Category != null)
                    {
                      foreach (var category in status.Information.Category)
                      {
                        var match = System.Text.RegularExpressions.Regex.Match(
                          category.Key,
                          @"^\s*(.*?)\s*\[Program\s+(\d+)\]\s*$",
                          System.Text.RegularExpressions.RegexOptions.CultureInvariant);

                        if (!match.Success || !int.TryParse(match.Groups[2].Value, out var programNumber))
                        {
                          continue;
                        }

                        var metadata = category.Value;
                        metadata.TryGetValue("Publisher", out var providerName);
                        metadata.TryGetValue("Type", out var serviceType);

                        var descriptor = new ServiceDescriptor
                        {
                          ServiceName = match.Groups[1].Value.Trim(),
                          ProviderName = providerName ?? string.Empty,
                          ProgramNumber = programNumber,
                          ServisType = (byte)(string.Equals(serviceType, "FM Radio", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(serviceType, "Radio", StringComparison.OrdinalIgnoreCase)
                            ? DVBTDriverServiceType.Radio
                            : DVBTDriverServiceType.TV),
                          Free = true
                        };

                        // VLC status.json does not expose the PMT PID; the program number
                        // is the only stable service-level identifier available here.
                        serviceDescriptors[descriptor] = programNumber;
                      }
                    }

                    if (serviceDescriptors.Count > 0)
                    {
                      StopReadBuffer();
                      return new DVBTDriverSearchProgramMapPIDsResult
                      {
                        Result = DVBTDriverSearchProgramResultEnum.OK,
                        ServiceDescriptors = serviceDescriptors
                      };
                    }
                  }

                    /*


                    {
  "fullscreen":0,
  "subtitledelay":0,
  "equalizer":[],
  "videoeffects":{
    "saturation":1,
    "gamma":1,
    "contrast":1,
    "hue":0,
    "brightness":1
  },
  "length":0,
  "currentplid":4,
  "seek_sec":10,
  "volume":0,
  "time":372,
  "version":"3.0.23 Vetinari",
  "stats":{
    "displayedpictures":0,
    "decodedvideo":0,
    "demuxbitrate":4.1372137069702,
    "averageinputbitrate":0,
    "inputbitrate":4.637659072876,
    "demuxreadbytes":1536648697,
    "averagedemuxbitrate":0,
    "readpackets":805843,
    "demuxdiscontinuity":0,
    "lostpictures":0,
    "decodedaudio":0,
    "sentbytes":0,
    "sentpackets":0,
    "readbytes":1730409904,
    "playedabuffers":0,
    "demuxreadpackets":0,
    "demuxcorrupted":0,
    "sendbitrate":0,
    "lostabuffers":0
  },
  "state":"playing",
  "loop":false,
  "information":{
    "title":0,
    "chapters":[],
    "category":{
      "Stream 23":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"289",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      " Nova Lady [Program 526]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      " Prima sport [Program 797]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "DB Test 2 [Program 1585]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      " Nova Krimi [Program 518]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 22":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Type":"Audio",
        "Description":"Audio description for the visually impaired",
        "Original_ID":"1299"
      },
      "Stream 17":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"274",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 0":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"1345",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 19":{
        "Language":"Czech",
        "Codec":"DVB Subtitles (dvbs)",
        "Type":"Subtitle",
        "Description":"DVB subtitles: hearing impaired",
        "Original_ID":"278"
      },
      "Stream 13":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"354",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 34":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"338",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "PRAHA TV [Program 8202]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 47":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"1042",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 18":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Type":"Audio",
        "Description":"Audio description for the visually impaired",
        "Original_ID":"275"
      },
      "Stream 4":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Type":"Audio",
        "Description":"Audio description for the visually impaired",
        "Original_ID":"323"
      },
      "Stream 24":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"290",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 44":{
        "Original_ID":"307",
        "Description":"Audio description for the visually impaired",
        "Type":"Audio",
        "Codec":"HEAD"
      },
      "Stream 12":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"353",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "ABC TV [Program 6923]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Slager muzika [Program 5641]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 2":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"321",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "RELAX [Program 2817]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Televize pres antenu [Program 33031]":{
        "Now_Playing":"Můj oblíbený televizní program",
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 7":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"3362",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 20":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"1297",
        "Orientation":"Top left",
        "Video_resolution":"1920x1080",
        "Buffer_dimensions":"1920x1080"
      },
      "DB Test 1 [Program 1537]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 38":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"785",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 14":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Type":"Audio",
        "Description":"Audio description for the visually impaired",
        "Original_ID":"355"
      },
      "Radio Cas Rock [Program 17926]":{
        "Type":"FM Radio",
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 21":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"1298",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "REBEL [Program 2818]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 27":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"1313",
        "Orientation":"Top left",
        "Video_resolution":"1920x1080",
        "Buffer_dimensions":"1920x1080"
      },
      "Stream 32":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"1458",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 33":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"337",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 50":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"3857",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 48":{
        "Original_ID":"1043",
        "Description":"Audio description for the visually impaired",
        "Type":"Audio",
        "Codec":"HEAD"
      },
      "Stream 37":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"3634",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 1":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"1346",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 8":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"1809",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 26":{
        "Language":"Czech",
        "Codec":"DVB Subtitles (dvbs)",
        "Type":"Subtitle",
        "Description":"DVB subtitles: hearing impaired",
        "Original_ID":"294"
      },
      "Stream 39":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"786",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 5":{
        "Language":"Czech",
        "Codec":"DVB Subtitles (dvbs)",
        "Type":"Subtitle",
        "Description":"DVB subtitles: hearing impaired",
        "Original_ID":"326"
      },
      " Nova [Program 525]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "JOJ Family [Program 2562]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 42":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"305",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 16":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"273",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 40":{
        "Language":"Original audio",
        "Codec":"HEAD",
        "Original_ID":"788",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 9":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"1810",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      " Nova Action [Program 515]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 43":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"306",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 25":{
        "Original_ID":"291",
        "Description":"Audio description for the visually impaired",
        "Type":"Audio",
        "Codec":"HEAD"
      },
      "Stream 35":{
        "Language":"Czech",
        "Codec":"DVB Subtitles (dvbs)",
        "Type":"Subtitle",
        "Description":"DVB subtitles: hearing impaired",
        "Original_ID":"342"
      },
      "Stream 30":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"2321",
        "Orientation":"Top left",
        "Video_resolution":"1920x1080",
        "Buffer_dimensions":"1920x1080"
      },
      "Stream 6":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"3361",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 3":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"322",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 36":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"3633",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 11":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"3378",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 45":{
        "Language":"Czech",
        "Codec":"DVB Subtitles (dvbs)",
        "Type":"Subtitle",
        "Description":"DVB subtitles: hearing impaired",
        "Original_ID":"310"
      },
      "meta":{
        "filename":"frequency=658000000",
        "publisher":"DB, MUX 24"
      },
      "Stream 51":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"3858",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 52":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"3633",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 49":{
        "Language":"Czech",
        "Codec":"DVB Subtitles (dvbs)",
        "Type":"Subtitle",
        "Description":"DVB subtitles",
        "Original_ID":"1046"
      },
      "Stream 10":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"3377",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      "Stream 28":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"1314",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Radio Cas [Program 17925]":{
        "Type":"FM Radio",
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 31":{
        "Type":"Audio",
        "Codec":"HEAD",
        "Original_ID":"2322",
        "Decoded_sample_rate":"24000 Hz"
      },
      " Nova Cinema [Program 524]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 53":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"3634",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Stream 46":{
        "Color_primaries":"ITU-R BT.709",
        "Codec":"MPEG-H Part2/HEVC (H.265) (hevc)",
        "Color_transfer_function":"ITU-R BT.709",
        "Color_space":"ITU-R BT.709 Range",
        "Frame_rate":"50",
        "Type":"Video",
        "Original_ID":"1041",
        "Orientation":"Top left",
        "Video_resolution":"960x540",
        "Buffer_dimensions":"960x544"
      },
      " Nova Fun [Program 517]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 41":{
        "Language":"Czech",
        "Codec":"DVB Subtitles (dvbs)",
        "Type":"Subtitle",
        "Description":"DVB subtitles: hearing impaired",
        "Original_ID":"790"
      },
      "Stream 29":{
        "Language":"Czech",
        "Codec":"HEAD",
        "Original_ID":"1442",
        "Type":"Audio",
        "Decoded_sample_rate":"24000 Hz"
      },
      "Slager original [Program 5640]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      },
      "Stream 15":{
        "Language":"Czech",
        "Codec":"DVB Subtitles (dvbs)",
        "Type":"Subtitle",
        "Description":"DVB subtitles: hearing impaired",
        "Original_ID":"358"
      },
      "CS Mystery [Program 6146]":{
        "Status":"Running",
        "Publisher":"DB, MUX 24"
      }
    },
    "chapter":0,
    "titles":[]
  },
  "repeat":false,
  "apiversion":3,
  "audiodelay":0,
  "random":false,
  "rate":1,
  "position":0,
  "audiofilters":{
    "filter_0":""
  }
}


                     */
                }
                catch (Exception e)
                {
                    _log.Debug($"Wrong data in Buffer");
                    ClearReadBuffer();
                    await Task.Delay(200);
                    continue;
                }

                await Task.Delay(200);
            }

            StopReadBuffer();

            return new DVBTDriverSearchProgramMapPIDsResult()
            {
                Result = DVBTDriverSearchProgramResultEnum.NoProgramFound
            };
        }

        public record VlcInformation(
        [property: JsonPropertyName("category")] Dictionary<string, Dictionary<string, string>>? Category
        );

        public record VlcStatusResponse(
        [property: JsonPropertyName("information")] VlcInformation? Information
        );

        public Task<DVBTDriverSearchPIDsResult> SearchProgramPIDs(long mapPID, bool setPIDsAndSync)
        {
            return Task.FromResult(new DVBTDriverSearchPIDsResult()
            {
                Result = DVBTDriverSearchProgramResultEnum.OK
            });
        }

        public Task<DVBTDriverSearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            return Task.FromResult(new DVBTDriverSearchAllPIDsResult()
            {
                Result = DVBTDriverSearchProgramResultEnum.OK
            });
        }

        public Task SetGain(GainEnum gain, int value = 0)
        {
            return Task.CompletedTask;
        }

        public Task<DVBTDriverResponse> SetPIDs(List<long> PIDs)
        {
            return Task.Run(async () =>
            {
                await TuneInternal(_lastTunedFreq, _lastTunedBandwidth, _lastTunedDeliverySystem, PIDs.FirstOrDefault());


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
            _recordingFileName = path;
            _recording = true;
        }

        public void StartStream()
        {
            _streaming = true;
        }

        public async Task<bool> Stop()
        {
            _log.Debug("RemoteVLCDriverConnector: Stopping stream");
            try
            {
                _streaming = false;
                _driverStreamDataAvailable = false;

                // Stop and clear VLC playlist
                await SendRequestAsync("requests/status.json?command=pl_stop");
                await SendRequestAsync("requests/status.json?command=pl_empty");

                // Clean up VLM instance if any was running
                await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("control tv stop")}");
                await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("del tv")}");

                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "RemoteVLCDriverConnector: Stop error");
                return false;
            }
        }

        public string StopRecording()
        {
            _recording = false;
            return _recordingFileName ?? "";
        }

        public void StopStream()
        {
            _streaming = false;
        }

        public async Task<DVBTDriverResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            var res = await TuneEnhanced(frequency, bandwidth, deliverySystem, false);
            return new DVBTDriverResponse()
            {
                SuccessFlag = res.Result == DVBTDriverSearchProgramResultEnum.OK
            };
        }

        public async Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            return await TuneInternal(frequency, bandWidth, deliverySystem);
        }

        private async Task<DVBTDriverTuneResult> TuneInternal(long frequency, long bandWidth, int deliverySystem, long programNumber = -1)
        {
            var res = new DVBTDriverTuneResult();

            try
            {
                _lastTunedFreq = frequency;
                _lastTunedBandwidth = bandWidth;
                _lastTunedDeliverySystem = deliverySystem;

                // Delivery system: dvb-t or dvb-t2
                string scheme = deliverySystem == 0 ? "dvb-t" : "dvb-t2";

                // Bandwidth in MHz
                var bandWidthMhz = (int)(bandWidth / 1E+6);
                if (bandWidthMhz <= 0)
                {
                    bandWidthMhz = 8;
                }

                string input = $"{scheme}://frequency={frequency}";

                // Resolve destination IP and port
                string targetHost = GetLocalIPAddressForRemote(_IP);
                int targetPort = _UDPStreamer?.Port ?? 1234;

                // Force mux=ts with all tracks and preserving original PIDs
                string sout = $"#std{{access=udp,mux=ts,dst={targetHost}:{targetPort}}}";

                _log.Info($"RemoteVLCDriverConnector: TuneInternal -> input: {input}, programNumber: {programNumber}, sout: {sout}");

                // 1. Stop and clean up any existing playlist playback / VLM instances
                await Stop();

                // 2. Start playing directly in VLC playlist via in_play so metadata tables are populated in status.json
                var urlBuilder = new StringBuilder();
                urlBuilder.Append($"requests/status.json?command=in_play&input={Uri.EscapeDataString(input)}");
                urlBuilder.Append($"&option={Uri.EscapeDataString($":dvb-bandwidth={bandWidthMhz}")}");

                if (programNumber > 0)
                {
                    urlBuilder.Append($"&option={Uri.EscapeDataString($":program={programNumber}")}");
                }
                else
                {
                    urlBuilder.Append($"&option={Uri.EscapeDataString(":program-numbers")}");
                    urlBuilder.Append($"&option={Uri.EscapeDataString(":dvb-sout-all")}");
                    urlBuilder.Append($"&option={Uri.EscapeDataString(":sout-all")}");
                }

                urlBuilder.Append($"&option={Uri.EscapeDataString(":dvb-sdt-parser")}");
                urlBuilder.Append($"&option={Uri.EscapeDataString(":ts-es-id-pid")}");
                urlBuilder.Append($"&option={Uri.EscapeDataString($":sout={sout}")}");

                string? response = await SendRequestAsync(urlBuilder.ToString());

                if (response != null)
                {
                    _streaming = true;
                    _driverStreamDataAvailable = true;
                    res.Result = DVBTDriverSearchProgramResultEnum.OK;
                }
                else
                {
                    _log.Error("RemoteVLCDriverConnector: in_play command failed: null response");
                    res.Result = DVBTDriverSearchProgramResultEnum.Error;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "RemoteVLCDriverConnector: TuneInternal error");
                res.Result = DVBTDriverSearchProgramResultEnum.Error;
            }

            return res;
        }

        public Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000)
        {
            return Task.CompletedTask;
        }

        public Task<DVBTDriverTuneResult> WaitForSignal(bool fastTuning)
        {
            return Task.FromResult(new DVBTDriverTuneResult()
            {
                Result = _driverStreamDataAvailable ? DVBTDriverSearchProgramResultEnum.OK : DVBTDriverSearchProgramResultEnum.NoSignal
            });
        }
    }
}
