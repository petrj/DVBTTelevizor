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
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.IO;
using LoggerService;
using System.Runtime.InteropServices;

namespace DVBTTelevizor
{
    public class DVBTDriverManager
    {
        DVBTTelevizorConfiguration _configuration;

        ILoggingService _log;

        TcpClient _controlClient;
        TcpClient _transferClient;
        NetworkStream _controlStream;
        NetworkStream _recordStream;

        bool _recording = false;
        bool _redingBuffer = false;

        List<byte> _readBuffer = new List<byte>();

        private static object _readThreadLock = new object();
        private static object _infoLock = new object();

        private string _dataStreamInfo  = "Data reading not initialized";

        public DVBTDriverManager()
        {
            Configuration = new DVBTTelevizorConfiguration();

            _log = new BasicLoggingService(LoggingLevelEnum.Debug);
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

        public List<byte> Buffer
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _readBuffer;
                }
            }
        }

        public void Start()
        {
            _controlClient = new TcpClient();
            _controlClient.Connect("127.0.0.1", _configuration.Driver.ControlPort);
            _controlStream = _controlClient.GetStream();

            //_client.NoDelay = true;
            //_client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            _transferClient = new TcpClient();
            _transferClient.Connect("127.0.0.1", _configuration.Driver.TransferPort);
            _recordStream = _transferClient.GetStream();

            var recordBackgroundWorker = new BackgroundWorker();
            recordBackgroundWorker.DoWork += worker_DoWork;
            recordBackgroundWorker.RunWorkerAsync();
        }

        public async Task Stop()
        {
            await SendCloseConnection();
            _controlClient.Close();
            _transferClient.Close();
        }

        public async Task StartRecording()
        {
            lock (_readThreadLock)
            {
                _recording = true;
            }
        }

        public void StopRecording()
        {
            lock (_readThreadLock)
            {
                _recording = false;
            }
        }

        public void StartReadBuffer()
        {
            lock (_readThreadLock)
            {
                _readBuffer.Clear();
                _redingBuffer = true;
            }
        }

        public void StopReadBuffer()
        {
            lock (_readThreadLock)
            {
                _redingBuffer = false;
            }
        }


        public async Task<DVBTResponse> SendRequest(DVBTRequest request, int secondsTimeout = 20)
        {
            var startTime = DateTime.Now;
            var bufferSize = 1024;

            return await Task.Run(() =>
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
                            _log.Debug("Reading from stream ...");

                            var readByteCount = _controlStream.Read(buffer, 0, bufferSize);
                            _log.Debug($"Reading from stream completed, bytes read: {readByteCount} ...");

                            if (readByteCount > 0)
                            {
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
                            _log.Debug("No data available ...");
                        }

                        var timeSpan = Math.Abs((DateTime.Now - startTime).TotalSeconds);
                        if (timeSpan > secondsTimeout)
                        {
                            throw new TimeoutException("TimeOut");
                        }

                        System.Threading.Thread.Sleep(200);
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

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                DataStreamInfo = "Reading data started ...";

                byte[] buffer = new byte[2048];                
                FileStream fs = null;
                bool rec = false;
                bool readingBuffer = false;
                string fName = null;

                DateTime lastBitRateMeasureStartTime = DateTime.Now;
                long bytesReadFromLastMeasureStartTime = 0;
                string lastSpeed = "";

                do
                {
                    lock (_readThreadLock)
                    {
                        // sync reading record state
                        rec = _recording;
                        readingBuffer = _redingBuffer;
                    }

                    string status = "Reading";
                    if (rec)
                    {
                        status += " and recording";
                        if (fs != null)
                        {
                            status += $" ({fName})";
                        }
                    }
                    if (readingBuffer)
                    {
                        status += " and reading buffer";                        
                    }

                    if (_transferClient.Available > 0)
                    {
                        var bytesRead = _recordStream.Read(buffer, 0, buffer.Length);
                        bytesReadFromLastMeasureStartTime += bytesRead;

                        _log.Debug($"Bytes read: {bytesRead} ...");

                        if (rec)
                        {
                            if (fs == null)
                            {
                                fName = "/storage/emulated/0/Download/" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "-DVBT-raw-stream.ts";
                                fs = new FileStream(fName, FileMode.Create, FileAccess.Write);
                            }

                            fs.Write(buffer, 0, bytesRead);
                        }
                        if (readingBuffer)
                        {
                            for (var i = 0; i < bytesRead; i++)
                                Buffer.Add(buffer[i]);
                        }

                         _transferClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    } else
                    {
                        _log.Debug($"No data on transfer port...");

                        status += " (no data)";

                        System.Threading.Thread.Sleep(200);
                    }

                    if (!rec && fs != null)
                    {
                        fs.Flush();
                        fs.Close();
                        fs = null;
                    }

                    // calculating speed:
                    var totalSeconds = (DateTime.Now - lastBitRateMeasureStartTime).TotalSeconds;
                    if (totalSeconds > 2)
                    {
                        var bytesPerSec = bytesReadFromLastMeasureStartTime*8 / totalSeconds;
                        string speed;
                        if (bytesPerSec < 1000)
                        {
                            speed = $", {bytesPerSec} b/sec";
                        } else
                        {
                            speed = $", {Convert.ToInt32(bytesPerSec/1000.0)} Kb/sec";
                        }

                        status += speed;

                        if (totalSeconds>3)
                        {
                            lastSpeed = speed;
                            lastBitRateMeasureStartTime = DateTime.Now;
                        }
                    } else
                    {
                        if (lastSpeed != String.Empty)
                        {
                            status += lastSpeed;
                        }
                        else
                        {
                            status += ", 0 b/sec";
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

        public DVBTTelevizorConfiguration Configuration
        {
            get
            {
                return _configuration;
            }
            set
            {
                _configuration = value;
            }
        }

        public async Task<DVBTStatus> GetStatus()
        {
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

            return status;
        }

        public async Task<DVBTVersion> GetVersion()
        {
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

            return version;
        }

        public async Task<DVBTResponse> Tune(long frequency, long bandwidth, int deliverySyetem)
        {
            // 26 bytes

            //List<byte> bytesToSend = new List<byte>();

            //bytesToSend.Add(2); // REQ_TUNE
            //bytesToSend.Add(3); // Payload for 3 longs

            //bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(frequency)); // Payload[0] => frequency
            //bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(bandwidth)); // Payload[1] => bandWidth
            //bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(deliverySyetem));         // Payload[2] => DeliverySystem DVBT

            var payload = new List<long>() { frequency, bandwidth, Convert.ToInt64(deliverySyetem) };

            var responseSize = 10;

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_TUNE, payload, responseSize);
            var response = await SendRequest(req, 10);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            return new DVBTResponse()
            {
                SuccessFlag = successFlag == 1,
                RequestTime = response.RequestTime,
                ResponseTime = response.ResponseTime
            };
        }

        public async Task<DVBTResponse> SendCloseConnection()
        {
            var responseSize = 10;

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_EXIT, new List<long>(), responseSize);
            var response = await SendRequest(req, 5);

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            return new DVBTResponse()
            {
                SuccessFlag = successFlag == 1,
                RequestTime = response.RequestTime,
                ResponseTime = response.ResponseTime
            };
        }

        public async Task<DVBTResponse> SetPIDs(List<long> PIDs)
        {
            var responseSize = 10;

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_SET_PIDS, PIDs, responseSize);
            var response = await SendRequest(req, 5);

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            return new DVBTResponse()
            {
                SuccessFlag = successFlag == 1,
                RequestTime = response.RequestTime,
                ResponseTime = response.ResponseTime
            };
        }

        public async Task<DVBTCapabilities> GetCapabalities()
        {
            var cap = new DVBTCapabilities();

            var responseSize = 2 + 7 * 8;

            var req = new DVBTRequest(DVBTDriverRequestTypeEnum.REQ_GET_CAPABILITIES, new List<long>(), responseSize);
            var response = await SendRequest(req, 5);

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            cap.ParseFromByteArray(response.Bytes.ToArray(), 2);

            cap.RequestTime = response.RequestTime;
            cap.ResponseTime = response.ResponseTime;

            return cap;
        }
    }
}
