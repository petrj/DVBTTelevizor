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

            StartReadBuffer();

            await Task.Delay(200);

            SDTTable sdtTable = null;
            PSITable psiTable = null;
            Dictionary<ServiceDescriptor, long> serviceDescriptors = null;

            List<MPEGTransportStreamPacket> packets = null;

            while ((DateTime.Now - startTime).TotalSeconds < timeoutForReadingBuffer)
            {
                // searching for PID 0 (PSI) and 17 (SDT) packets ..

                try
                {
                    var data = GetReadBufferData();
                    packets = MPEGTransportStreamPacket.Parse(data);

                    /*
                    var pid0packets = MPEGTransportStreamPacket.GetAllPacketsPayloadBytesByPID(packets, 0);
                    var pid17packets = MPEGTransportStreamPacket.GetAllPacketsPayloadBytesByPID(packets, 17);

                    if (pid0packets.Count>0 || pid17packets.Count>0)
                    {
                        _log.Info("0, 17");
                        var sdtTable2 = DVBTTable.CreateFromPackets<SDTTable2>(packets, 17);
                    }
                    */

                    sdtTable = DVBTTable.CreateFromPackets<SDTTable>(packets, 17);
                    psiTable = DVBTTable.CreateFromPackets<PSITable>(packets, 0);

                }
                catch (Exception e)
                {
                    _log.Debug($"Wrong data in Buffer");
                    ClearReadBuffer();
                    await Task.Delay(200);
                    continue;
                }

                if (sdtTable != null && psiTable != null)
                {
                    // does SDT table belongs to this frequency?
                    serviceDescriptors = MPEGTransportStreamPacket.GetAvailableServicesMapPIDs(sdtTable, psiTable);

                    if (serviceDescriptors.Count > 0)
                    {
                        break;
                    }
                    else
                    {
                        _log.Debug($"Wrong SDTTable in buffer!");
                        ClearReadBuffer();
                    }
                }

                await Task.Delay(200);
            }

            StopReadBuffer();

            /*
            try
            {
                // Query standard HTTP status endpoint (contains active input media information)
                string? xmlResponse = await SendRequestAsync("requests/status.xml");

                if (string.IsNullOrWhiteSpace(xmlResponse))
                {
                    // Fall back to querying VLM state directly if main status is empty
                    xmlResponse = await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("show tv")}");
                }

                if (string.IsNullOrWhiteSpace(xmlResponse))
                {

                }
            } catch (Exception e)
            {

            };
            */
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

        private string? GetVlmError(string? response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return "Empty response";
            }

            var match = System.Text.RegularExpressions.Regex.Match(response, @"<error>(.*?)</error>", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (match.Success)
            {
                var msg = match.Groups[1].Value.Trim();
                return string.IsNullOrEmpty(msg) ? null : msg;
            }

            if (response.Contains("<error/>"))
            {
                return null;
            }

            return null;
        }

        public async Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            var res = new DVBTDriverTuneResult();

            try
            {
                _lastTunedFreq = frequency;

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


                _log.Info($"RemoteVLCDriverConnector: TuneEnhanced -> input: {input}, sout: {sout}");

                // 1. Stop existing stream before reconfiguring
                await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("control tv stop")}");
                await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("del tv")}");

                // 2. Setup broadcast stream
                var newRes = await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("new tv broadcast enabled")}");
                var newErr = GetVlmError(newRes);
                if (newErr != null && !newErr.Contains("Name already in use", StringComparison.OrdinalIgnoreCase))
                {
                    _log.Error($"RemoteVLCDriverConnector: VLM 'new tv' failed: {newErr}");
                }

                // Set input MRL: dvb-t2://frequency=658000000
                await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString($"setup tv input {input}")}");

                // Set options to retain raw stream tables and preserve original PIDs
                await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString($"setup tv option dvb-bandwidth={bandWidthMhz}")}");
               // await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("setup tv option demux=ts")}");
                //await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("setup tv option ts-extra-pmt=0x0,0x11")}");

                // Tells DVB tuner driver not to filter out SI/PSI PIDs at the hardware/tuner layer
               // await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("setup tv option dvb-sout-all")}");

                // Tells VLC core pipeline to process all tracks and pass metadata tables downstream
               // await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("setup tv option sout-all")}");

                // Enable DVB SDT and PSI table parsing for VLM
                //await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("setup tv option dvb-sdt-parser")}");
                //await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("setup tv option program-numbers")}");

                // Preserves original PID mapping
               // await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("setup tv option ts-es-id-pid")}");

                // Set stream output
                await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString($"setup tv output {sout}")}");

                // 3. Start streaming
                string? response = await SendRequestAsync($"requests/vlm_cmd.xml?command={Uri.EscapeDataString("control tv play")}");
                var playErr = GetVlmError(response);

                if (response != null && playErr == null)
                {
                    _streaming = true;
                    _driverStreamDataAvailable = true;
                    res.Result = DVBTDriverSearchProgramResultEnum.OK;
                }
                else
                {
                    _log.Error($"RemoteVLCDriverConnector: VLM control play failed: {playErr ?? "null response"}");
                    res.Result = DVBTDriverSearchProgramResultEnum.Error;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "RemoteVLCDriverConnector: TuneEnhanced error");
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
