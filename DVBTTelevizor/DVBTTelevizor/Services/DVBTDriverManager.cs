using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.IO;
using LoggerService;
using System.Runtime.InteropServices;
using MPEGTS;
using DVBTTelevizor.Models;
using Android.Net.Sip;
using SQLite;
using DVBTTelevizor.Services;
using Java.IO;
using System.Threading;

namespace DVBTTelevizor
{
    public class DVBTDriverManager : IDVBTDriverManager
    {
        public bool Installed { get; set; } = true;

        DVBTDriverConfiguration _driverConfiguration;

        ILoggingService _log;

        private TcpClient _controlClient;
        private TcpClient _transferClient;
        private NetworkStream _controlStream;
        private NetworkStream _transferStream;
        private DVBTTelevizorConfiguration _config;

        private long _lastTunedFreq = -1;
        private long _lastTunedDeliverySystem = -1;
        private long _bitrate = 0;
        private List<long> _lastPIDs = new List<long>();

        private const int ReadBufferSize = 32768;

        private bool _readingStream = true;
        private bool _streaming = false;
        private bool _recording = false;
        private bool _readingBuffer = false;
        private bool _driverStreamDataAvailable = false;
        private string _recordingFileName = null;

        List<byte> _readBuffer = new List<byte>();
        private string _lastSpeedCalculationSec = null;

        private static object _readThreadLock = new object();
        private static object _infoLock = new object();

        private string _dataStreamInfo = "Data reading not initialized";

        private DVBUDPStreamer _DVBUDPStreamer;

        public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);
        public event EventHandler StatusChanged;

        public DVBTDriverManager(ILoggingService loggingService, DVBTTelevizorConfiguration config)
        {
            _log = loggingService;

            _log.Debug($"Initializing DVBT driver manager");

            _config = config;

            _DVBUDPStreamer = new DVBUDPStreamer(_log);
        }

        public DVBTDriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return _readingStream ? DVBTDriverStreamTypeEnum.UDP : DVBTDriverStreamTypeEnum.Stream;
            }
        }

        public bool DriverStreamDataAvailable
        {
            get
            {
                return _driverStreamDataAvailable;
            }
        }

        public string StreamUrl
        {
            get
            {
                if (_DVBUDPStreamer == null)
                {
                    return "udp://@localhost:1234";
                }

                return $"udp://@{_DVBUDPStreamer.IP}:{_DVBUDPStreamer.Port}";
            }
        }

        public long Bitrate
        {
            get
            {
                return _bitrate;
            }
        }

        public Stream VideoStream
        {
            get
            {
                // play raw video from driver Transfer stream
                return _transferStream;
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

        public bool Connected
        {
            get
            {
                return _controlClient != null && _controlClient.Connected;
            }
        }

        public long LastTunedFreq
        {
            get
            {
                return _lastTunedFreq;
            }
        }

        public void Connect()
        {
            _log.Debug($"Starting");

            _controlClient = new TcpClient();
            _controlClient.Connect("127.0.0.1", _driverConfiguration.ControlPort);
            _controlStream = _controlClient.GetStream();

            _lastTunedFreq = -1;
            _lastTunedDeliverySystem = -1;

            StartBackgroundReading();
        }

        private void StartBackgroundReading()
        {
            _log.Debug($"Starting background reading");

            _transferClient = new TcpClient();
            _transferClient.Connect("127.0.0.1", _driverConfiguration.TransferPort);
            _transferStream = _transferClient.GetStream();

            var recordBackgroundWorker = new BackgroundWorker();
            recordBackgroundWorker.DoWork += worker_DoWork;
            recordBackgroundWorker.RunWorkerAsync();
        }

        private void StopBackgroundReading()
        {
            _log.Debug($"Stopping background reading");

            _transferClient.Close();
        }

        private void StartReadStream()
        {
            lock (_readThreadLock)
            {
                _log.Debug($"Starting read stream");

                _readingStream = true;
            }
        }

        private void StopReadStream()
        {
            lock (_readThreadLock)
            {
                _log.Debug($"Stopping read stream");

                _readingStream = false;
            }
        }

        public void StartStream()
        {
             _log.Debug($"PlayStream");

            try
            {
                _streaming = true;
                _driverStreamDataAvailable = false;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void StopStream()
        {
            _log.Debug($"StopStream");

            try
            {
                _streaming = false;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public async Task Disconnect()
        {
            _log.Debug($"Dsconnecting");

            try
            {
                await SendCloseConnection();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while sending close connection");
            }

            try
            {
                _controlClient.Close();
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while closing control client");
            }

            StopBackgroundReading();
        }

        public async Task StartRecording()
        {
            lock (_readThreadLock)
            {
                _log.Debug($"starting recording");

                if (!Recording)
                {
                    _recording = true;
                }
            }
        }

        public void StopRecording()
        {
            lock (_readThreadLock)
            {
                if (Recording)
                {
                    _log.Debug($"Stopping recording");
                    _recording = false;
                    _recordingFileName = null;
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
                _log.Debug($"clearing buffer");

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

        public async Task<bool> Stop()
        {
            _log.Debug("Stopping ...");

            //StartReadStream();
            StopStream();

            //var setPIDsRes = await SetPIDs(new List<long>() { 0, 16, 17 });
            var setPIDsRes = await SetPIDs(new List<long>() { });
            if (!setPIDsRes.SuccessFlag)
                return false;

            return true;
        }

        private async Task<DVBTResponse> SendRequest(DVBTRequest request, int secondsTimeout = 20)
        {
            _log.Debug($"Sending request {request}");

            var startTime = DateTime.Now;
            var bufferSize = 1024;

            return await Task.Run(async () =>
            {
                var _responseBuffer = new List<byte>();

                try
                {
                    byte[] buffer = new byte[bufferSize];

                    bool reading = true;

                    request.Send(_controlStream);

                    do
                    {
                        if (_controlClient.Client.Available > 0)
                        {
                            //_log.Debug("Reading from stream ...");

                            var readByteCount = _controlStream.Read(buffer, 0, bufferSize);

                            if (readByteCount > 0)
                            {
                                _log.Debug($"Received {readByteCount} bytes");

                                for (var i = 0; i < readByteCount; i++)
                                {
                                    _responseBuffer.Add(buffer[i]);
                                }
                                if (request.ResponseBytesExpectedCount >= _responseBuffer.Count)
                                {
                                    reading = false;
                                }
                            }
                        }
                        else
                        {
                            //_log.Debug("No data available ...");
                        }

                        var timeSpan = Math.Abs((DateTime.Now - startTime).TotalSeconds);
                        if (timeSpan > secondsTimeout)
                        {
                            throw new TimeoutException("TimeOut");
                        }

                        await Task.Delay(200);
                    }
                    while (reading);

                }
                catch (TimeoutException)
                {
                    _log.Debug("Timeout for reading data ...");
                    return new DVBTResponse() { SuccessFlag = false };
                }
                catch (Exception ex)
                {
                    _log.Debug($"General error: {ex}");
                    return new DVBTResponse() { SuccessFlag = false };
                }

                _log.Debug($"Request sent");

                var response = new DVBTResponse()
                {
                    SuccessFlag = true,
                    RequestTime = startTime,
                    ResponseTime = DateTime.Now
                };
                response.Bytes.AddRange(_responseBuffer);
                return response;
            });
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

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            _log.Debug("Starting DVBT reader thread");

            var totalBytesRead = 0;
            _bitrate = 0;

            try
            {
                DataStreamInfo = "";

                byte[] buffer = new byte[ReadBufferSize];

                FileStream recordFileStream = null;
                long bytesReadFromLastMeasureStartTime = 0;

                bool readingStream = true;
                bool rec = false;
                bool readingBuffer = false;
                bool streaming = false;

                DateTime lastBitRateMeasureStartTime = DateTime.Now;

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
                    } else
                    {
                        status = $"Not tuned";
                    }

                    if (!readingStream)
                    {
                        status += ", not reading";
                        System.Threading.Thread.Sleep(200);
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

                        if (_transferClient.Available > 0)
                        {
                            var bytesRead = _transferStream.Read(buffer, 0, buffer.Length);
                            totalBytesRead += bytesRead;
                            bytesReadFromLastMeasureStartTime += bytesRead;


                            if (rec)
                            {
                                if (recordFileStream == null)
                                {
                                    var fileNameFreq = (_lastTunedFreq / 1000000).ToString() + "MHz";
                                    _recordingFileName = Path.Combine(BaseViewModel.AndroidAppDirectory, $"DVBT-MPEGTS-{fileNameFreq}-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.ts");

                                    if (System.IO.File.Exists(_recordingFileName))
                                        System.IO.File.Delete(_recordingFileName);

                                    recordFileStream = new FileStream(_recordingFileName, FileMode.Create, FileAccess.Write);
                                }

                                recordFileStream.Write(buffer, 0, bytesRead);
                            }
                            if (readingBuffer)
                            {
                                for (var i = 0; i < bytesRead; i++)
                                    _readBuffer.Add(buffer[i]);
                            }
                            if (streaming)
                            {
                                _DVBUDPStreamer.SendByteArray(buffer, bytesRead);

                                if (!_driverStreamDataAvailable && bytesRead > 0)
                                {
                                    _driverStreamDataAvailable = true;
                                    _log.Debug("DVBT driver data available");
                                }
                            }

                            _transferClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        }
                        else
                        {
                            System.Threading.Thread.Sleep(100);
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

                                status += $"({BaseViewModel.GetHumanReadableBitRate(_bitrate)})";
                            }

                            _log.Debug($"{status}");

                            bytesReadFromLastMeasureStartTime = 0;
                            _lastSpeedCalculationSec = currentLastSpeedCalculationSec;
                        }
                    }

                    DataStreamInfo = status;

                }
                while (_transferClient.Connected);

            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error while reading from TransferPort");
            }

            _log.Debug($"Reading data finished");
            DataStreamInfo = "Reading data finished";
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

        public async Task<bool> CheckStatus()
        {
            _log.Debug("Checking status");

            try
            {
                if (!Connected)
                    return false;

                var status = await GetStatus();

                if (!status.SuccessFlag)
                {
                    return false;
                }

                return true;

             } catch (Exception ex)
            {
                _log.Error(ex, "Error while checking status");
                return false;
            }
        }

        public async Task<DVBTStatus> GetStatus()
        {
            _log.Debug("Getting status");

            var status = new DVBTStatus();

            /*
                    (long) snr, // parameter 1
                    (long) bitErrorRate, // parameter 2
                    (long) droppedUsbFps, // parameter 3
                    (long) rfStrengthPercentage, // parameter 4
                    hasSignal ? 1L : 0L, // parameter 5
                    hasCarrier ? 1L : 0L, // parameter 6
                    hasSync ? 1L : 0L, // parameter 7
                    hasLock ? 1L : 0L // parameter 8
            */

            var responseSize = 2 + 9 * 8;

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_GET_STATUS, new List<long>(), responseSize);

            var response = await SendRequest(req);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];

            status.ParseFromByteArray(response.Bytes.ToArray(), 2);
            status.RequestTime = response.RequestTime;
            status.ResponseTime = response.ResponseTime;

            _log.Debug($"Status response: {status.ToString()}");

            if (StatusChanged != null)
            {
                StatusChanged(this, new StatusChangedEventArgs() { Status = status });
            }

            return status;
        }

        public async Task<DVBTVersion> GetVersion()
        {
            _log.Debug($"Getting version");

            var version = new DVBTVersion();

            var responseSize = 26;  // 1 + 1 + 8 + 8 + 8

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_PROTOCOL_VERSION, new List<long>(), responseSize);
            var response = await SendRequest(req, 5);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];

            var ar = response.Bytes.ToArray();

            version.SuccessFlag = DVBTStatus.GetBigEndianLongFromByteArray(ar, 2) == 1;
            version.Version = DVBTStatus.GetBigEndianLongFromByteArray(ar, 10);
            version.AllRequestsLength = DVBTStatus.GetBigEndianLongFromByteArray(ar, 18);

            version.RequestTime = response.RequestTime;
            version.ResponseTime = response.ResponseTime;

            _log.Debug($"Version response: {version.ToString()}");

            return version;
        }

        public async Task<DVBTResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            _log.Debug($"Tuning {frequency} MHz, type: {deliverySystem}");

            try
            {
                var payload = new List<long>() { frequency, bandwidth, Convert.ToInt64(deliverySystem) };

                var responseSize = 10;

                var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_TUNE, payload, responseSize);
                var response = await SendRequest(req, 10);

                if (response.Bytes.Count < responseSize)
                    throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count}");

                var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

                if (successFlag == 1)
                {
                    _lastTunedFreq = frequency;
                    _lastTunedDeliverySystem = deliverySystem;
                }

                _log.Debug($"Tune response: {successFlag}");

                return new DVBTResponse()
                {
                    SuccessFlag = successFlag == 1,
                    RequestTime = response.RequestTime,
                    ResponseTime = response.ResponseTime
                };
            } catch (Exception ex)
            {
                _log.Error(ex, "Tune error");

                return new DVBTResponse()
                {
                    SuccessFlag = false
                };
            }
        }

        private async Task<DVBTResponse> SendCloseConnection()
        {
            _log.Debug($"Closing connection");

            var responseSize = 10;

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_EXIT, new List<long>(), responseSize);
            var response = await SendRequest(req, 5);

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            _log.Debug($"Close connection response: {successFlag}");

            return new DVBTResponse()
            {
                SuccessFlag = successFlag == 1,
                RequestTime = response.RequestTime,
                ResponseTime = response.ResponseTime
            };
        }

        public async Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000)
        {
            _log.Debug($"--------------------------------------------------------------------");
            _log.Debug($"Wait For Buffer PIDs: {String.Join(",", PIDs)}");

            try
            {
                StartReadBuffer();

                // waiting
                await Task.Delay(readMsTimeout);

                var pidsFound = new Dictionary<long, int>();
                var wrongPIDsFound = new Dictionary<long, int>();

                var startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds < msTimeout)
                {
                    var buffer = GetReadBufferData();

                    _log.Debug($"Buffer size: {buffer.Length}");

                    var packets = MPEGTransportStreamPacket.Parse(buffer);

                    _log.Debug($"Packets found: {packets.Count}");

                    var packetsByPID = MPEGTransportStreamPacket.SortPacketsByPID(packets);

                    foreach (var pidPackets in packetsByPID)
                    {
                        if (PIDs.Contains(pidPackets.Key))
                        {
                            pidsFound[pidPackets.Key] = pidPackets.Value.Count;
                        } else
                        {
                            wrongPIDsFound[pidPackets.Key] = pidPackets.Value.Count;
                        }
                    }

                    _log.Debug($"--Found:");
                    foreach (var pids in pidsFound)
                    {
                        _log.Debug($"  PID  : {pids.Key.ToString().PadLeft(10)} {pids.Value.ToString().PadLeft(10)}(x)");
                    }
                    if (wrongPIDsFound.Count > 0)
                    {
                        _log.Debug($"--Wrong:");
                        foreach (var pids in wrongPIDsFound)
                        {
                            _log.Debug($"  PID  : {pids.Key.ToString().PadLeft(10)} {pids.Value.ToString().PadLeft(10)}(x)");
                        }
                    }

                    if (pidsFound.Count == PIDs.Count &&
                        wrongPIDsFound.Count == 0)
                    {
                        _log.Debug($"All PIDs found");
                        return;
                    }

                    await Task.Delay(readMsTimeout);

                    ClearReadBuffer();
                    pidsFound.Clear();
                    wrongPIDsFound.Clear();
                }

                _log.Debug($"Wait for PIDs timeout!");

            } catch (Exception ex)
            {
                _log.Error(ex);
            } finally
            {
                StopReadBuffer();
            }
        }

        public async Task<DVBTResponse> SetPIDs(List<long> PIDs)
        {
            try
            {

                _log.Debug($"Setting PIDs: {String.Join(",", PIDs)}");
                _lastPIDs = PIDs;

                var responseSize = 10;

                var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_SET_PIDS, PIDs, responseSize);
                var response = await SendRequest(req, 5);

                if (response.Bytes.Count < responseSize)
                    throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count}");

                var requestNumber = response.Bytes[0];
                var longsCountInResponse = response.Bytes[1];
                var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

                _log.Debug($"Set PIDS response: {successFlag}");

                return new DVBTResponse()
                {
                    SuccessFlag = successFlag == 1,
                    RequestTime = response.RequestTime,
                    ResponseTime = response.ResponseTime
                };
            } catch (Exception ex)
            {
                return new DVBTResponse()
                {
                    SuccessFlag = false
                };
            }
        }

        private void SaveBuffer(string namePrefix, byte[] buffer)
        {
            var fileName = Path.Combine(BaseViewModel.AndroidAppDirectory, $"{namePrefix}.{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}.dat");
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush();
                fs.Close();
            }
        }

        public async Task<DVBTCapabilities> GetCapabalities()
        {
            _log.Debug($"Getting capabilities");

            var cap = new DVBTCapabilities();

            var responseSize = 2 + 7 * 8;

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_GET_CAPABILITIES, new List<long>(), responseSize);
            var response = await SendRequest(req, 5);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            cap.ParseFromByteArray(response.Bytes.ToArray(), 2);

            cap.RequestTime = response.RequestTime;
            cap.ResponseTime = response.ResponseTime;

            _log.Debug($"Capabilities response: {cap.ToString()}");

            return cap;
        }

        public async Task<SearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            var PIDsAsString = String.Join(",", MapPIDs);
            _log.Debug($"Searching PIDS of Map PIDs: {PIDsAsString}");

            var res = new SearchAllPIDsResult();

            try
            {
                // setting PIDs filter

                var pidRes = await SetPIDs(MapPIDs);

                if (!pidRes.SuccessFlag)
                {
                    _log.Debug($"Setting PIDs {PIDsAsString} failed");
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                // getting status

                var status = await GetStatus();

                if (!status.SuccessFlag)
                {
                    _log.Debug($"Getting status failed");
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                if (status.hasSignal != 1 || status.hasSync != 1 || status.hasLock != 1)
                {
                    _log.Debug($"No signal");
                    res.Result = SearchProgramResultEnum.NoSignal;
                    return res;
                }

                var pmtTables = new Dictionary<long,PMTTable>();

                try
                {
                    StartReadBuffer();

                    // waiting
                    await Task.Delay(1000);

                    var timeoutForReadingBuffer = 15; //  seconds timeout for getting PMT
                    var startTime = DateTime.Now;

                    while ((DateTime.Now - startTime).TotalSeconds < timeoutForReadingBuffer)
                    {
                        var allPackets = MPEGTransportStreamPacket.Parse(GetReadBufferData());

                        foreach (var mapPID in MapPIDs)
                        {
                            var tbl = DVBTTable.CreateFromPackets<PMTTable>(allPackets, mapPID);
                            if (tbl != null)
                            {
                                pmtTables[mapPID] = tbl;
                            }
                        }

                        if (pmtTables.Count == MapPIDs.Count)
                        {
                            break;
                        }

                        await Task.Delay(500);
                    }
                }
                finally
                {
                    StopReadBuffer();
                    ClearReadBuffer();
                }

                if (pmtTables.Count == 0)
                {
                    _log.Debug($"No PMT found");
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                //SaveBuffer($"ProgramPID.{MapPID.ToString()}", pmtPacketBytes.ToArray());

                res.Result = SearchProgramResultEnum.OK;

                foreach (var kvp in pmtTables)
                {
                    res.PIDs[kvp.Key] = new List<long>();

                    foreach (var stream in kvp.Value.Streams)
                    {
                        res.PIDs[kvp.Key].Add(stream.PID);
                    }
                }

                _log.Debug($"Searching PIDS response: {res}");

                return res;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                res.Result = SearchProgramResultEnum.Error;
                return res;
            }
        }

        public async Task<SearchPIDsResult> SearchProgramPIDs(long mapPID)
        {
            _log.Debug($"Searching PIDS of Map PID: {mapPID}");

            var res = new SearchPIDsResult();

            try
            {
                // setting PIDs filter

                var pidRes = await SetPIDs(new List<long>() { mapPID });

                if (!pidRes.SuccessFlag)
                {
                    _log.Debug($"Setting PID {mapPID} failed");
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                // getting status

                var status = await GetStatus();

                if (!status.SuccessFlag)
                {
                    _log.Debug($"Getting status failed");
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                if (status.hasSignal != 1 || status.hasSync != 1 || status.hasLock != 1)
                {
                    _log.Debug($"No signal");
                    res.Result = SearchProgramResultEnum.NoSignal;
                    return res;
                }

                PMTTable pmtTable = null;

                try
                {
                    StartReadBuffer();

                    // waiting
                    await Task.Delay(100);

                    var timeoutForReadingBuffer = 10000; //  10 s timeout for getting PMT
                    var startTime = DateTime.Now;

                    while ((DateTime.Now - startTime).TotalMilliseconds < timeoutForReadingBuffer)
                    {
                        var allPackets = MPEGTransportStreamPacket.Parse(GetReadBufferData());
                        if (allPackets.Count > 0)
                        {
                            pmtTable = DVBTTable.CreateFromPackets<PMTTable>(allPackets, mapPID);

                            if (pmtTable != null)
                            {
                                break;
                            }
                        }

                        await Task.Delay(100);
                    }
                }
                finally
                {
                    StopReadBuffer();
                    ClearReadBuffer();
                }

                if (pmtTable == null)
                {
                    _log.Debug($"No PMT found");
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                res.Result = SearchProgramResultEnum.OK;

                foreach (var stream in pmtTable.Streams)
                {
                    _log.Debug($"PIDS found: {stream.PID}");
                    res.PIDs.Add(stream.PID);
                }

                _log.Debug($"Searching PIDS response: {res}");

                return res;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                res.Result = SearchProgramResultEnum.Error;
                return res;
            }
        }

        public async Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            _log.Debug($"Testing driver data");

            try
            {
                StartReadBuffer();

                var startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds < readMsTimeout)
                {
                    // waiting
                    await Task.Delay(50);

                    if (BufferContainsData())
                    {
                        _log.Debug($"Non zero buffer (after {(DateTime.Now - startTime).TotalSeconds} ms)");
                        return true;
                    }
                }

                _log.Debug($"No data in Buffer!");
                return false;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                return false;
            }
            finally
            {
                ClearReadBuffer();
                StopReadBuffer();
            }
        }

        /// <summary>
        /// Tuning with timeout (setting PIDs 0,17,18)
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="bandWidth"></param>
        /// <param name="deliverySystem"></param>
        /// <param name="fastTuning"></param>
        /// <returns></returns>
        public async Task<TuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            _log.Debug($"Tuning enhanced freq: {frequency} MHz, type: {deliverySystem} fastTuning: {fastTuning}");

            TuneResult res = null;
            DVBTResponse tuneRes = null;
            DVBTResponse setPIDres = null;

            double getSignalTime = 0;
            double testDataTime = 0;
            double tuneTime = 0;
            double setPIDsTime = 0;

            var attemptsCount = fastTuning ? 2 : 6;
            var tuneAttemptsCount = fastTuning ? 3 : 6;

            var tuningStartTime = DateTime.Now;
            var PIDs = new List<long>() { 0, 17, 18 };

            try
            {
                for (var j = 1; j < tuneAttemptsCount; j++)
                {
                    var startTuneTime = DateTime.Now;

                    for (var i = 1; i < attemptsCount; i++)
                    {
                        tuneRes = await Tune(frequency, bandWidth, deliverySystem);

                        if (tuneRes.SuccessFlag)
                        {
                            _log.Debug($"Tune response time: {(tuneRes.ResponseTime - tuneRes.RequestTime).TotalMilliseconds} ms");
                            break;
                        }
                        else
                        {
                            await Task.Delay(fastTuning ? 50 : 100);
                        }
                    }

                    tuneTime += (DateTime.Now - startTuneTime).TotalMilliseconds;

                    if (!tuneRes.SuccessFlag)
                    {
                        res.Result = SearchProgramResultEnum.Error;
                        return res;
                    }

                    var startSetPIDsStartTime = DateTime.Now;

                    // set PIDs 0 and 17
                    for (var i = 1; i <= attemptsCount; i++)
                    {
                        setPIDres = await SetPIDs(PIDs);

                        if (setPIDres.SuccessFlag)
                        {
                            _log.Debug($"SetPIDs response time: {(setPIDres.ResponseTime - setPIDres.RequestTime).TotalMilliseconds} ms");
                            break;
                        }
                        else
                        {
                            await Task.Delay(fastTuning ? 50 : 100);
                        }
                    }

                    setPIDsTime += (DateTime.Now - startSetPIDsStartTime).TotalMilliseconds;

                    if (!setPIDres.SuccessFlag)
                    {
                        res.Result = SearchProgramResultEnum.Error;
                        return res;
                    }

                    var getSignalStartTime = DateTime.Now;

                    res = await WaitForSignal(fastTuning);

                    if (res.Result != SearchProgramResultEnum.OK)
                    {
                        return res;
                    }

                    getSignalTime += (DateTime.Now - getSignalStartTime).TotalMilliseconds;

                    var testDataStartTime = DateTime.Now;

                    var driverSendingData = await DriverSendingData(fastTuning ? 500 : 1000);

                    testDataTime += (DateTime.Now - testDataStartTime).TotalMilliseconds;

                    if (driverSendingData)
                    {
                        break;
                    }
                    else
                    {
                        _log.Info("No data after PIDs set and signal locked");
                        //await Tune(0, bandWidth, deliverySystem);
                    }
                }

                return res;
            }
            catch (Exception ex)
            {
                _log.Error(ex);

                res.Result = SearchProgramResultEnum.Error;
                return res;
            }
            finally
            {
                var totalTime = (DateTime.Now - tuningStartTime).TotalMilliseconds;

                _log.Debug($"-----------Tuning {(frequency/1000).ToString("N0")} MHz ---------------------");
                _log.Debug($"Tune:                   {tuneTime.ToString("N2").PadLeft(20, ' ')} ms");
                _log.Debug($"Set PIDs:               {setPIDsTime.ToString("N2").PadLeft(20, ' ')} ms");
                _log.Debug($"Get signal:             {getSignalTime.ToString("N2").PadLeft(20, ' ')} ms");
                _log.Debug($"Test data:              {testDataTime.ToString("N2").PadLeft(20, ' ')} ms");
                _log.Debug($"-----------------------------------------------------");
                _log.Debug($"Tuning total time:      {totalTime.ToString("N2").PadLeft(20, ' ')} ms");
                _log.Debug($"-----------------------------------------------------");
            }
        }

        public async Task<TuneResult> WaitForSignal(bool fastTuning)
        {
            TuneResult res = new TuneResult() { Result = SearchProgramResultEnum.NoSignal };

            // timeout for get signal:
            var startTime = DateTime.Now;

            var totalTimeoutforSignalSeconds = fastTuning ? 3 : 10;
            var timeoutforSignalLockSeconds = fastTuning ? 2 : 5;

            DVBTStatus status = null;

            while ((DateTime.Now - startTime).TotalSeconds < totalTimeoutforSignalSeconds)
            {
                status = await GetStatus();
                res.SignalState = status;

                if (!res.SignalState.SuccessFlag)
                {
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                if (status.hasSignal == 0 && status.hasCarrier == 0 && (DateTime.Now - startTime).TotalSeconds > timeoutforSignalLockSeconds)
                {
                    res.Result = SearchProgramResultEnum.NoSignal;
                    break;
                }

                if (status.hasSignal == 1 && status.hasSync == 1 && status.hasLock == 1)
                {
                    res.Result = SearchProgramResultEnum.OK;
                    break;
                }

                // waiting
                await Task.Delay(fastTuning ? 400 : 850);
            }

            if (status.hasSignal != 1 || status.hasSync != 1 || status.hasLock != 1)
            {
                res.Result = SearchProgramResultEnum.NoSignal;
                return res;
            }

            return res;
        }

        /// <summary>
        /// Set up channles PIDs by map PID
        /// </summary>
        /// <param name="mapPID"></param>
        /// <param name="fastTuning"></param>
        /// <returns></returns>
        public async Task<TuneResult> SetupChannelPIDs(long mapPID, bool fastTuning)
        {
            _log.Debug($"Set up channle PIDs for mapPID: {mapPID}, fastTuning: {fastTuning}");

            double searchPIDsTime = 0;
            double setPIDsTime = 0;

            var startTime = DateTime.Now;

            try
            {
                var res = new TuneResult();

                var searchPIDsStartTime = DateTime.Now;

                // set Map PID for getting PMT table
                var pmtTableSearchRes = await SearchProgramPIDs(mapPID);

                if (pmtTableSearchRes.Result != SearchProgramResultEnum.OK)
                {
                    res.Result = pmtTableSearchRes.Result;
                    return res;
                }

                searchPIDsTime += (DateTime.Now - searchPIDsStartTime).TotalMilliseconds;

                var setPIDsStartTime = DateTime.Now;

                pmtTableSearchRes.PIDs.Add(0);  // PAT
                pmtTableSearchRes.PIDs.Add(16); // NIT
                pmtTableSearchRes.PIDs.Add(17); // SDT
                pmtTableSearchRes.PIDs.Add(18); // EIT
                pmtTableSearchRes.PIDs.Add(20); // TDT
                pmtTableSearchRes.PIDs.Add(mapPID);

                DVBTResponse setPIDres = null;

                setPIDres = await SetPIDs(pmtTableSearchRes.PIDs);

                if (!setPIDres.SuccessFlag)
                {
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                setPIDsTime += (DateTime.Now - setPIDsStartTime).TotalMilliseconds;

                res.Result = SearchProgramResultEnum.OK;
                return res;
            } finally
            {
                var totalTime = (DateTime.Now - startTime).TotalMilliseconds;

                _log.Debug($"-----------Set PIDs for MapPID {mapPID} ---------------------");
                _log.Debug($"Search:                 {searchPIDsTime.ToString("N2").PadLeft(20, ' ')} ms");
                _log.Debug($"Set PIDs:               {setPIDsTime.ToString("N2").PadLeft(20, ' ')} ms");
                _log.Debug($"-----------------------------------------------------");
                _log.Debug($"Total time:             {totalTime.ToString("N2").PadLeft(20, ' ')} ms");
                _log.Debug($"-----------------------------------------------------");
            }
        }

        /// <summary>
        /// EPG scan
        /// </summary>
        /// <param name="msTimeout"></param>
        /// <returns></returns>
        public async Task<EITScanResult> ScanEPG(int msTimeout = 2000)
        {
            _log.Debug($"Scanning EPG from Buffer");

            try
            {
                // searching for PID 18 (EIT) + PSI packets ..
                StartReadBuffer();

                await Task.Delay(msTimeout);

                StopReadBuffer();

                var eitService = new EITService(_log);
                var scanResult = eitService.Scan(MPEGTransportStreamPacket.Parse(GetReadBufferData()));

                return scanResult;
            }
            catch (Exception ex)
            {
                _log.Error(ex);

                return new EITScanResult()
                {
                    OK = false
                };
            } finally
            {
                ClearReadBuffer();
            }
        }

        public async Task<SearchMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            _log.Debug($"Searching Program Map PIDs");

            var res = new SearchMapPIDsResult();

            try
            {
                if (tunePID0and17)
                {
                    // setting PID filter

                    var pids = new List<long>() { 0, 16, 17 };
                    var pidRes = await SetPIDs(pids);

                    if (!pidRes.SuccessFlag)
                    {
                        res.Result = SearchProgramResultEnum.Error;
                        return res;
                    }
                }

                var timeoutForReadingBuffer = 15; //  15 secs
                var startTime = DateTime.Now;

                StartReadBuffer();

                await Task.Delay(200);

                SDTTable sdtTable = null;
                PSITable psiTable = null;
                Dictionary<ServiceDescriptor, long> serviceDescriptors = null;

                List<MPEGTransportStreamPacket> packets = null;

                while ((DateTime.Now-startTime).TotalSeconds < timeoutForReadingBuffer)
                {
                    // searching for PID 0 (PSI) and 17 (SDT) packets ..

                    try
                    {
                        packets = MPEGTransportStreamPacket.Parse(GetReadBufferData());

                        sdtTable = DVBTTable.CreateFromPackets<SDTTable>(packets, 17);
                        psiTable = DVBTTable.CreateFromPackets<PSITable>(packets, 0);

                    } catch (Exception e)
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
                        } else
                        {
                            _log.Debug($"Wrong SDTTable in buffer!");
                            ClearReadBuffer();
                        }
                    }

                    await Task.Delay(200);
                }

                StopReadBuffer();

                // debug save buffer
                //SaveBuffer($"Buffer.{_lastTunedFreq}", Buffer.ToArray());

                if (sdtTable == null || psiTable == null)
                {
                    _log.Debug($"No SDT or PSI table found");

                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                res.ServiceDescriptors = serviceDescriptors;
                res.Result = SearchProgramResultEnum.OK;

                _log.Debug($"SDT descriptors:");
                foreach (var sdt in sdtTable.ServiceDescriptors)
                {
                    _log.Debug($"Name: {sdt.ProviderName}, provider: {sdt.ProviderName}, number: {sdt.ProgramNumber}");
                }

                _log.Debug($"PSI association:");
                foreach (var pr in psiTable.ProgramAssociations)
                {
                    _log.Debug($"MapPID: {pr.ProgramMapPID}, number: {pr.ProgramNumber}");
                }

                _log.Debug($"Searching Program Map PIDS response: {res.Result}");

                return res;

            } catch (Exception ex)
            {
                _log.Error(ex);

                res.Result = SearchProgramResultEnum.Error;
                return res;
            }
        }
    }
}
