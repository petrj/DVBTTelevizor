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

namespace DVBTTelevizor
{
    public class DVBTDriverManager : IDVBTDriverManager
    {
        DVBTDriverConfiguration _driverConfiguration;

        ILoggingService _log;

        private TcpClient _controlClient;
        private TcpClient _transferClient;
        private NetworkStream _controlStream;
        private NetworkStream _transferStream;
        private DVBTTelevizorConfiguration _config;

        private long _lastTunedFreq = -1;
        private long _lastTunedDeliverySystem = -1;
        private List<long> _lastPIDs = new List<long>();

        private const int ReadBufferSize = 32768;

        private bool _readingStream = true;
        private bool _streaming = false;
        private bool _recording = false;
        private bool _readingBuffer = false;
        private string _recordingFileName = null;

        List<byte> _readBuffer = new List<byte>();
        private string lastSpeedCalculationSec = null;

        private static object _readThreadLock = new object();
        private static object _infoLock = new object();

        private string _dataStreamInfo = "Data reading not initialized";

        private DVBUDPStreamer _DVBUDPStreamer;


        public DVBTDriverManager(ILoggingService loggingService, DVBTTelevizorConfiguration config)
        {
            _log = loggingService;

            _log.Debug($"Initializing DVBT driver manager");

            _config = config;

            _DVBUDPStreamer = new DVBUDPStreamer(_log);
        }

        public string StreamUrl
        {
            get
            {
                if (_DVBUDPStreamer == null)
                {
                    return "udp://@localhost:9600";
                }

                return $"udp://@{_DVBUDPStreamer.IP}:{_DVBUDPStreamer.Port}";
            }
        }


        public Stream VideoStream
        {
            get
            {
                // play raw video from driver Transfer stream
                if (!_readingStream)
                {
                    return _transferStream;
                }

                return null;
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

        private List<byte> Buffer
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _readBuffer;
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
                _log.Debug($"Stopping read buffer (total bytes found: {Buffer.Count})");

                _readingBuffer = false;
            }
        }

        public async Task<bool> Stop()
        {
            _log.Debug("Stopping ...");

            //StartReadStream();
            StopStream();

            var setPIDsRes = await SetPIDs(new List<long>() { 0, 16, 17 });
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
                                    Buffer.Add(buffer[i]);
                            }
                            if (streaming)
                            {
                                _DVBUDPStreamer.SendByteArray(buffer, bytesRead);
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

                        if (lastSpeedCalculationSec != currentLastSpeedCalculationSec)
                        {
                            // occurs once per second

                            if (bytesReadFromLastMeasureStartTime > 0)
                            {
                                lastSpeedCalculationSec = currentLastSpeedCalculationSec;
                                var bitsPerSec = bytesReadFromLastMeasureStartTime * 8;

                                if (bitsPerSec > 1000000)
                                {
                                    status += $" ({Convert.ToInt32((bitsPerSec / 1000000.0)).ToString("N0")} Mb/sec)";
                                }
                                else if (bitsPerSec > 1000)
                                {
                                    status += $" ({Convert.ToInt32((bitsPerSec / 1000.0)).ToString("N0")} Kb/sec)";
                                }
                                else
                                {
                                    status += $" ({bitsPerSec} b/sec)";
                                }
                            }

                            _log.Debug($"{status}");

                            bytesReadFromLastMeasureStartTime = 0;
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

            if (frequency == _lastTunedFreq && deliverySystem == _lastTunedDeliverySystem)
            {
                _log.Debug($"Frequency already tuned");

                return new DVBTResponse()
                {
                    SuccessFlag = true,
                    RequestTime = DateTime.Now,
                    ResponseTime = DateTime.Now
                };
            }

            // 26 bytes

            //List<byte> bytesToSend = new List<byte>();

            //bytesToSend.Add(2); // REQ_TUNE
            //bytesToSend.Add(3); // Payload for 3 longs

            //bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(frequency)); // Payload[0] => frequency
            //bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(bandwidth)); // Payload[1] => bandWidth
            //bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(deliverySyetem));         // Payload[2] => DeliverySystem DVBT

            var payload = new List<long>() { frequency, bandwidth, Convert.ToInt64(deliverySystem) };

            var responseSize = 10;

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_TUNE, payload, responseSize);
            var response = await SendRequest(req, 10);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

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

        public async Task WaitForBufferPIDs(List<long> PIDs, int msTimeout = 3000)
        {
            _log.Debug($"--------------------------------------------------------------------");
            _log.Debug($"Wait For Buffer PIDs: {String.Join(",", PIDs)}");

            var pidsFound = new Dictionary<long, int>();
            var wrongPIDsFound = new Dictionary<long, int>();
            foreach (var pid in PIDs)
            {
                pidsFound.Add(pid, 0);
            }

            try
            {
                var startTime = DateTime.Now;

                while ((DateTime.Now - startTime).TotalMilliseconds < msTimeout)
                {
                    _log.Debug($"Buffer size: {Buffer.Count}");

                    var packets = MPEGTransportStreamPacket.Parse(Buffer);

                    _log.Debug($"Packets found: {packets.Count}");

                    var packetsByPID = MPEGTransportStreamPacket.SortPacketsByPID(packets);

                    foreach (var pidPackets in packetsByPID)
                    {
                        //_log.Debug($"  PID  : {pidPackets.Key.ToString().PadLeft(10)} {pidPackets.Value.Count.ToString().PadLeft(10)}(x)");

                        if (!pidsFound.ContainsKey(pidPackets.Key))
                        {
                            if (!wrongPIDsFound.ContainsKey(pidPackets.Key))
                            {
                                wrongPIDsFound.Add(pidPackets.Key, 0);
                            }
                            wrongPIDsFound[pidPackets.Key] = pidPackets.Value.Count;
                        }
                        else
                        {
                            pidsFound[pidPackets.Key] = pidPackets.Value.Count;
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


                    await Task.Delay(100);
                }

            } catch (Exception ex)
            {
                _log.Error(ex);
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
                        var allPackets = MPEGTransportStreamPacket.Parse(Buffer);

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

        /// <summary>
        /// Tuning with timeout
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="bandWidth"></param>
        /// <param name="deliverySystem"></param>
        /// <param name="PIDs"></param>
        /// <param name="fastTuning"></param>
        /// <returns></returns>
        public async Task<TuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, List<long> PIDs, bool fastTuning)
        {
            _log.Debug($"Tuning enhanced freq: {frequency} MHz, type: {deliverySystem} fastTuning: {fastTuning}");

            var res = new TuneResult();

            try
            {
                DVBTResponse tuneRes = null;
                DVBTResponse setPIDres = null;

                var attemptsCount = fastTuning ? 1 : 5;

                // five attempts
                for (var i = 1; i <= attemptsCount; i++)
                {
                    tuneRes = await Tune(frequency, bandWidth, deliverySystem);

                    if (tuneRes.SuccessFlag)
                    {
                        break;
                    } else
                    {
                        if (!fastTuning)
                        await Task.Delay(500);
                    }
                }

                if (!tuneRes.SuccessFlag)
                {
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                // set PIDs 0 and 17
                for (var i = 1; i <= attemptsCount; i++)
                {
                    setPIDres = await SetPIDs(PIDs);

                    if (setPIDres.SuccessFlag)
                    {
                        break;
                    }
                    else
                    {
                        if (!fastTuning)
                            await Task.Delay(100);
                    }
                }

                if (!setPIDres.SuccessFlag)
                {
                    res.Result = SearchProgramResultEnum.Error;
                    return res;
                }

                // freq tuned

                // timeout for get signal:
                var startTime = DateTime.Now;

                var totalTimeoutforSignalSeconds = fastTuning ? 3 : 10;
                var timeoutforSignalLockSeconds = fastTuning ? 2 : 5;

                DVBTStatus status = new DVBTStatus();

                while ((DateTime.Now - startTime).TotalSeconds < totalTimeoutforSignalSeconds)
                {
                    status = await GetStatus();

                    if (!status.SuccessFlag)
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

                res.SignalPercentStrength = status.rfStrengthPercentage;

                return res;

            }
            catch (Exception ex)
            {
                _log.Error(ex);

                res.Result = SearchProgramResultEnum.Error;
                return res;
            }
        }

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
                var scanResult = eitService.Scan(MPEGTransportStreamPacket.Parse(Buffer));

                return scanResult;
            }
            catch (Exception ex)
            {
                _log.Error(ex);

                return new EITScanResult()
                {
                    OK = false
                };
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

                StartReadBuffer();

                var timeoutForReadingBuffer = 15; //  15 secs
                var startTime = DateTime.Now;

                SDTTable sdtTable = null;
                PSITable psiTable = null;
                Dictionary<ServiceDescriptor, long> serviceDescriptors = null;

                while ((DateTime.Now-startTime).TotalSeconds < timeoutForReadingBuffer)
                {
                    // searching for PID 0 (PSI) and 17 (SDT) packets ..

                    var packets = MPEGTransportStreamPacket.Parse(Buffer);

                    sdtTable = DVBTTable.CreateFromPackets<SDTTable>(packets, 17);
                    psiTable = DVBTTable.CreateFromPackets<PSITable>(packets, 0);

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

                    await Task.Delay(500);
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
