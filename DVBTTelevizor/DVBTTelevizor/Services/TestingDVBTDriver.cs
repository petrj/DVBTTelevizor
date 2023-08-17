using LoggerService;
using Newtonsoft.Json;
using RemoteAccessService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DVBTTelevizor.Services
{
    public class TestingDVBTDriver
    {
        public long SupportedDeliverySystems { get; set; }
        public long MinFrequency { get; set; } = 474000000;
        public long MaxFrequency { get; set; } = 714000000;
        public long FrequencyStepSize { get; set; } = 8;
        public long VendorId { get; set; } = 0;
        public long ProductId { get; set; } = 0;

        public bool SendingDataDisabled { get; set; } = false;

        private BackgroundWorker _controlWorker;
        private BackgroundWorker _transferWorker;

        private const int MaxBufferSize = 1250000;

        private IPEndPoint _controlIPEndPoint = null;
        private IPEndPoint _transferIPEndPoint = null;

        private ILoggingService _loggingService;

        private long _frequency = 0;
        private long _bandWidth = 0;
        private long _deliverySystem = 0;

        private long _sendingDataFrequency = 0;
        private int _sendingDataPosition = 0;
        private Dictionary<long, List<byte>> _freqStreams = null;

        private List<long> _PIDFilter = new List<long>();

        private object key = 1;
        private string _lastSpeedCalculationSec = null;

        public TestingDVBTDriver(ILoggingService loggingService)
        {
            _loggingService = loggingService;

            _controlWorker = new BackgroundWorker();
            _controlWorker.WorkerSupportsCancellation = true;
            _controlWorker.DoWork += _controlWorker_DoWork;

            _transferWorker = new BackgroundWorker();
            _transferWorker.WorkerSupportsCancellation = true;
            _transferWorker.DoWork += _transferWorker_DoWork;

            _freqStreams = new Dictionary<long, List<byte>>();
        }

        public void Connect()
        {
            var ipAddress = IPAddress.Parse("127.0.0.1");
            var controlPort = DVBUDPStreamer.FindAvailablePort(32000, 33000);
            var transferPort = DVBUDPStreamer.FindAvailablePort(42000, 43000);

            _controlIPEndPoint = new IPEndPoint(ipAddress, controlPort);
            _transferIPEndPoint = new IPEndPoint(ipAddress, transferPort);

            _controlWorker.RunWorkerAsync();
            _transferWorker.RunWorkerAsync();
        }

        public void Disconnect()
        {
            _controlWorker.CancelAsync();
            _transferWorker.CancelAsync();
        }

        public IPEndPoint ControlIPEndPoint
        {
            get
            {
                return _controlIPEndPoint;
            }
        }

        public IPEndPoint TransferIPEndPoint
        {
            get
            {
                return _transferIPEndPoint;
            }
        }

        private void _transferWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var bytes = new Byte[MaxBufferSize];

                var bufferSize = 500000; // constant speed cca 4 MB

                _loggingService.Info($"TestingDVBTDriver transfer endpoint: {_transferIPEndPoint.Address}:{_transferIPEndPoint.Port}");

                using (var listener = new Socket(_transferIPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(_transferIPEndPoint);
                    listener.Listen(10);

                    using (var handler = listener.Accept())
                    {
                        while (!_transferWorker.CancellationPending)
                        {
                            // TODO:
                            //   -- send only filtered PIDs
                            //   -- read TDT and send data with calculated bitrate

                            lock (key)
                            {
                                if ((_sendingDataFrequency != 0) && (_freqStreams.ContainsKey(_sendingDataFrequency)) && _PIDFilter.Count>0 && !SendingDataDisabled)
                                {
                                    var currentLastSpeedCalculationSec = DateTime.Now.ToString("yyyyMMddhhmmss");
                                    if (_lastSpeedCalculationSec != currentLastSpeedCalculationSec)
                                    {
                                        // occurs once per second
                                        _loggingService.Debug($"TestingDVBTDriver sending data....");
                                        _lastSpeedCalculationSec = currentLastSpeedCalculationSec;

                                        if (_sendingDataPosition + bufferSize < _freqStreams[_sendingDataFrequency].Count)
                                        {
                                            _freqStreams[_sendingDataFrequency].CopyTo(_sendingDataPosition, bytes, 0, bufferSize);
                                            handler.Send(bytes, bufferSize, SocketFlags.None);

                                            _sendingDataPosition += bufferSize;
                                        }
                                        else
                                        {
                                            _sendingDataPosition = 0;
                                        }
                                    }
                                }
                            }

                            Thread.Sleep(100);
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                e.Cancel = true;
                Thread.ResetAbort();

                _loggingService.Info("Background thread aborted");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Background thread stopped");
            }
        }

        private byte[] GetCapabilities()
        {
            _loggingService.Info($"TestingDVBTDriver GetCapabilities");

            var bytesToSend = new List<byte>();

            var payload = new List<long>();

            payload.Add(1); // success
            payload.Add(SupportedDeliverySystems);
            payload.Add(MinFrequency);
            payload.Add(MaxFrequency);
            payload.Add(FrequencyStepSize);
            payload.Add(VendorId);
            payload.Add(ProductId);

            bytesToSend.Add((byte)DVBTDriverRequestTypeEnum.REQ_GET_CAPABILITIES);
            bytesToSend.Add(Convert.ToByte(payload.Count));

            foreach (var payloadItem in payload)
            {
                bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(payloadItem));
            }

            return bytesToSend.ToArray();
        }

        private byte[] GetStatus()
        {
            _loggingService.Info($"TestingDVBTDriver GetStatus");

            var bytesToSend = new List<byte>();

            var payload = new List<long>();

            payload.Add(1); // success

            lock(key)
            {

                if (_sendingDataFrequency == 0)
                {
                    payload.Add(0); // snr
                    payload.Add(0); // bitErrorRatectionpl
                    payload.Add(0); // droppedUsbFps
                    payload.Add(0); // rfStrengthPercentage
                    payload.Add(0); // hasSignal
                    payload.Add(0); // hasCarrier
                    payload.Add(0); // hasSync
                    payload.Add(0); // hasLock

                }
                else
                {
                    payload.Add(0); // snr
                    payload.Add(0); // bitErrorRate
                    payload.Add(0); // droppedUsbFps
                    payload.Add(100); // rfStrengthPercentage
                    payload.Add(1); // hasSignal
                    payload.Add(1); // hasCarrier
                    payload.Add(1); // hasSync
                    payload.Add(1); // hasLock
                }
            }

            bytesToSend.Add((byte)DVBTDriverRequestTypeEnum.REQ_GET_STATUS);
            bytesToSend.Add(Convert.ToByte(payload.Count));

            foreach (var payloadItem in payload)
            {
                bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(payloadItem));
            }

            return bytesToSend.ToArray();
        }

        public static List<byte> LoadBytesFromFile(string path)
        {
            byte[] buffer = new byte[188];
            var streamBytes = new List<byte>();

            using (var fs = new FileStream(path, FileMode.Open))
            {
                while (fs.Position + 188 < fs.Length)
                {
                    fs.Read(buffer, 0, 188);
                    streamBytes.AddRange(buffer);
                }
                fs.Close();
            }

            return streamBytes;
        }

        private byte[] Tune(byte[] request)
        {
            _frequency = DVBTResponse.GetBigEndianLongFromByteArray(request, 2);
            _bandWidth = DVBTResponse.GetBigEndianLongFromByteArray(request, 10);
            _deliverySystem = DVBTResponse.GetBigEndianLongFromByteArray(request, 18);

            _loggingService.Info($"TestingDVBTDriver Tuning: {_frequency / 1000000} MHz ({_bandWidth}/{_deliverySystem})");

            // looking for MPEGTS dump of this _frequency
            var folder = BaseViewModel.AndroidAppDirectory;
            var fNamePattern = "DVBT-MPEGTS-" + (_frequency / 1000000).ToString("N0") + "*.ts";
            var res = System.IO.Directory.GetFiles(folder, fNamePattern);

            lock(key)
            {

                if (res.Length > 0)
                {
                    if (!_freqStreams.ContainsKey(_frequency))
                    {
                        _freqStreams.Add(_frequency, null);
                    }

                    _freqStreams[_frequency] = LoadBytesFromFile(res[0]);

                    _sendingDataPosition = 0;
                    _sendingDataFrequency = _frequency;
                }
                else
                {
                    _sendingDataFrequency = 0;
                }
            }

            var bytesToSend = new List<byte>();

            bytesToSend.Add((byte)DVBTDriverRequestTypeEnum.REQ_TUNE);
            bytesToSend.Add(1);

            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(1)); // success flag

            _loggingService.Info($"TestingDVBTDriver Tuned");

            return bytesToSend.ToArray();
        }

        private byte[] SetPIDs(byte[] request)
        {
            var payLoadSize = request[1];

            lock (key)
            {
                _PIDFilter.Clear();

                for (var i = 0; i < payLoadSize; i++)
                {
                    var pid = DVBTResponse.GetBigEndianLongFromByteArray(request, 2 + i * 8);
                    _PIDFilter.Add(pid);
                }

                _loggingService.Info($"TestingDVBTDriver _PIDFilter: {string.Join(",", _PIDFilter)}");
            }

            var bytesToSend = new List<byte>();

            bytesToSend.Add((byte)DVBTDriverRequestTypeEnum.REQ_TUNE);
            bytesToSend.Add(1);

            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(1)); // success flag

            return bytesToSend.ToArray();
        }

        private void _controlWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Data buffer for incoming data.
                var buffer = new Byte[MaxBufferSize];
                var responseBuffer = new List<byte>();
                var totalBytesExpected = 2;

                DVBTDriverRequestTypeEnum? reqType = null;

                _loggingService.Info($"TestingDVBTDriver control endpoint: {_controlIPEndPoint.Address}:{_controlIPEndPoint.Port}");

                using (var listener = new Socket(_controlIPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(_controlIPEndPoint);
                    listener.Listen(10);

                    // Start listening for connections.
                    while (!_controlWorker.CancellationPending)
                    {
                        using (var handler = listener.Accept())
                        {
                            //var readStarted = DateTime.Now;

                            while (true)
                            {
                                int bytesRec = handler.Receive(buffer);

                                if (bytesRec > 0)
                                {
                                    for (var i = 0; i < bytesRec; i++)
                                    {
                                        responseBuffer.Add(buffer[i]);
                                    }

                                    if (responseBuffer.Count > 1 && reqType == null)
                                    {
                                        // first byte - request type
                                        reqType = (DVBTDriverRequestTypeEnum)responseBuffer[0];

                                        // second byte - payload size
                                        var payloadSize = responseBuffer[1];

                                        totalBytesExpected += payloadSize*8; // 1 long = 8 bytes
                                    }
                                }

                                if (totalBytesExpected == responseBuffer.Count)
                                {
                                    if (reqType.HasValue)
                                    {
                                        switch (reqType.Value)
                                        {
                                            case DVBTDriverRequestTypeEnum.REQ_EXIT:
                                                Disconnect();
                                                break;
                                            case DVBTDriverRequestTypeEnum.REQ_GET_CAPABILITIES:
                                                handler.Send(GetCapabilities());
                                                break;
                                            case DVBTDriverRequestTypeEnum.REQ_TUNE:
                                                handler.Send(Tune(responseBuffer.ToArray()));
                                                break;
                                            case DVBTDriverRequestTypeEnum.REQ_SET_PIDS:
                                                handler.Send(SetPIDs(responseBuffer.ToArray()));
                                                break;
                                            case DVBTDriverRequestTypeEnum.REQ_GET_STATUS:
                                                handler.Send(GetStatus());
                                                break;
                                        }
                                    }

                                    responseBuffer.Clear();
                                    totalBytesExpected = 2;
                                    reqType = null;
                                }
                                else
                                {
                                    // waiting for next bytes .....
                                    Thread.Sleep(100);
                                }
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                e.Cancel = true;
                Thread.ResetAbort();

                _loggingService.Info("Background thread aborted");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Background thread stopped");
            }
        }
    }
}
