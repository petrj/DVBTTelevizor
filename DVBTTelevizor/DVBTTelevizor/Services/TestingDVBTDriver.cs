using Java.Nio;
using LoggerService;
using MPEGTS;
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
using static System.Net.Mime.MediaTypeNames;

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
        private const int MinBufferSize = 1250;

        private IPEndPoint _controlIPEndPoint = null;
        private IPEndPoint _transferIPEndPoint = null;

        private ILoggingService _loggingService;

        private long _frequency = 0;
        private long _bandWidth = 0;
        private long _deliverySystem = 0;

        private long _sendingDataFrequency = 0;
        private int _sendingDataPosition = 0;
        private Dictionary<long, List<byte>> _freqStreams = null;
        private TimeSpan _timeShift;

        private List<long> _PIDFilter = new List<long>();

        private object key = 1;

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

        public static bool EncodingTest(byte[] bytes = null)
        {
            var ok = true;

            if (bytes == null)
                bytes = new byte[] { 65, 66, 67 };

            var index = 0;
            var count = 3;

            // Xamarin does not suport this encodings:
            // iso-8859-10: "'iso-8859-10' is not a supported encoding name. For information on defining a custom encoding, see the documentation for the Encoding.RegisterProvider method.\nParameter name: name"
            // iso-8859-13: {System.NotSupportedException: No data is available for encoding 28603. For information on defining a custom encoding, see the documentation for the Encoding.RegisterProvider method.  at ....

            foreach (var enc in new string[] {
                "iso-8859-1",
                "iso-8859-2",
                "iso-8859-3",
                "iso-8859-4",
                "iso-8859-5",
                "iso-8859-6",
                "iso-8859-7",
                "iso-8859-8",
                "iso-8859-9",
                "iso-8859-11",
                "iso-8859-15" })
            {
                try
                {
                    var txt = System.Text.Encoding.GetEncoding(enc).GetString(bytes, index, count);
                }
                catch (Exception ex)
                {
                    ok = false;
                }
            }

            try
            {
                var unicodeTest = System.Text.Encoding.Unicode.GetString(bytes, index, count);
            }
            catch (Exception ex)
            {
                ok = false;
            }

            try
            {
                var utf8Test = System.Text.Encoding.UTF8.GetString(bytes, index, count);
            }
            catch (Exception ex)
            {
                ok = false;
            }

            return ok;
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

        private long FindPCRPID(byte[] buffer)
        {
            try
            {
                _loggingService.Info("Finding PCR PID");

                SDTTable sDTTable = null;
                PSITable psiTable = null;
                PMTTable pmtTable = null;

                var allPackets = MPEGTransportStreamPacket.Parse(buffer);

                if (psiTable == null)
                {
                    psiTable = DVBTTable.CreateFromPackets<PSITable>(allPackets, 0);
                }
                if (sDTTable == null)
                {
                    sDTTable = DVBTTable.CreateFromPackets<SDTTable>(allPackets, 17);
                }
                if (pmtTable == null && psiTable != null && sDTTable != null)
                {
                    pmtTable = MPEGTransportStreamPacket.GetPMTTable(allPackets, sDTTable, psiTable);
                    if (pmtTable != null)
                    {
                        _loggingService.Info($"PCR PID found: {pmtTable.PCRPID}");
                        return pmtTable.PCRPID;
                    }
                }

            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error whiel getting PCR PID");
            }

            _loggingService.Info($"NO PCR PID found");
            return -1;
        }

        public ulong? GetFirstPCRClock(long PID, byte[] buffer)
        {
            try
            {
                if (buffer.Length < 188 * 2)
                    return null;

                var offset = 0;
                var pos = 0;

                while (pos < 188)
                {
                    if (
                            (buffer[pos] == MPEGTransportStreamPacket.MPEGTSSyncByte) &&
                            (buffer[pos + 188] == MPEGTransportStreamPacket.MPEGTSSyncByte)
                       )
                    {
                        offset = pos;
                    }
                    pos++;
                }

                while (offset + 188 <= buffer.Length)
                {
                    if (buffer[offset] != MPEGTransportStreamPacket.MPEGTSSyncByte)
                    {
                        throw new Exception("invalid packet, no sync byte found");
                    }

                    if (((buffer[offset + 1] & 0x1F) << 8) + buffer[offset + 2] == PID && (buffer[offset + 1] & 0x80) != 128)
                    {
                        AdaptationFieldControlEnum adaptationFieldControlEnum = (AdaptationFieldControlEnum)((buffer[offset + 3] & 0x30) >> 4);
                        if ((adaptationFieldControlEnum == AdaptationFieldControlEnum.AdaptationFieldFollowedByPayload || adaptationFieldControlEnum == AdaptationFieldControlEnum.AdaptationFieldOnlyNoPayload) && buffer[offset + 4] > 6 && (buffer[offset + 5] & 0x10) == 16)
                        {
                            return MPEGTransportStreamPacket.GetPCRClock(new List<byte>
                    {
                        buffer[offset + 6],
                        buffer[offset + 7],
                        buffer[offset + 8],
                        buffer[offset + 9],
                        buffer[offset + 10],
                        buffer[offset + 11]
                    }) / 27000000;
                        }
                    }

                    offset += 188;
                }

                return null;
            } catch (Exception ex)
            {
                _loggingService.Error(ex);
                return null;
            }
        }

        private string GetHumanReadableSize(double bytes, bool highPrecision = false)
        {
            var frm = highPrecision ? "N2" : "N0";

            if (bytes > 1000000)
            {
                return Math.Round(bytes / 1000000.0, 2).ToString(frm) + " MB";
            }

            if (bytes > 1000)
            {
                return Math.Round(bytes / 1000.0, 2).ToString(frm) + " kB";
            }

            return bytes.ToString(frm) + " B";
        }

        private int GetCorrectedBufferSize(int bufferSize)
        {
            // divisible by 188
            while (bufferSize % 188 != 0)
            {
                bufferSize++;
            }

            return bufferSize;
        }

        private void _transferWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var bytes = new Byte[MaxBufferSize];
                var bufferSize = 250000; // constant speed cca 4 MB
                var lastSpeedCalculationTime = DateTime.MinValue;
                var lastSpeedCalculationTimeLog = DateTime.MinValue;

                var frequencyPCRPID = new Dictionary<long, long>();
                var firstPCRTimeStamp = ulong.MinValue;
                var firstPCRTimeStampTime = DateTime.MinValue;

                var loopsPerSecond = 5;
                var PCR = "";

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

                            lock (key)
                            {
                                if ((_sendingDataFrequency != 0) && (_freqStreams.ContainsKey(_sendingDataFrequency)) && _PIDFilter.Count>0 && !SendingDataDisabled)
                                {
                                    if ((DateTime.Now-lastSpeedCalculationTime).TotalMilliseconds> (1000 / loopsPerSecond))
                                    {
                                        // calculate speed

                                        lastSpeedCalculationTime = DateTime.Now;

                                        var bitsPerSec = (bufferSize*5.0) * 8;
                                        var speed = "";

                                        if (bitsPerSec > 1000000)
                                        {
                                            speed = $" ({Convert.ToInt32((bitsPerSec / 1000000.0)).ToString("N0")} Mb/sec)";
                                        }
                                        else if (bitsPerSec > 1000)
                                        {
                                            speed = $" ({Convert.ToInt32((bitsPerSec / 1000.0)).ToString("N0")} Kb/sec)";
                                        }
                                        else
                                        {
                                            speed = $" ({bitsPerSec} b/sec)";
                                        }

                                        // sending data

                                        if (_sendingDataPosition + bufferSize < _freqStreams[_sendingDataFrequency].Count)
                                        {
                                            //_loggingService.Debug($"TestingDVBTDriver sending data....");

                                            var thisSecBytes = new byte[bufferSize];
                                            _freqStreams[_sendingDataFrequency].CopyTo(_sendingDataPosition, thisSecBytes, 0, bufferSize);

                                            handler.Send(thisSecBytes, bufferSize, SocketFlags.None);

                                            _sendingDataPosition += bufferSize;

                                            // calculating buffer size for balancing bitrate

                                            if (!frequencyPCRPID.ContainsKey(_sendingDataFrequency))
                                            {
                                                frequencyPCRPID[_sendingDataFrequency] = -1;
                                            }

                                            if (frequencyPCRPID[_sendingDataFrequency] == -1)
                                            {
                                                frequencyPCRPID[_sendingDataFrequency] = FindPCRPID(thisSecBytes);
                                            } else
                                            {
                                                var PCRPID = frequencyPCRPID[_sendingDataFrequency];

                                                var timeStamp = GetFirstPCRClock(PCRPID, thisSecBytes);

                                                if (timeStamp.HasValue && timeStamp.Value != ulong.MinValue)
                                                {
                                                    _loggingService.Debug($"Timestamp: {timeStamp}");

                                                    if (firstPCRTimeStamp == ulong.MinValue)
                                                    {
                                                        firstPCRTimeStamp = timeStamp.Value;
                                                        firstPCRTimeStampTime = DateTime.Now;
                                                    }
                                                    else
                                                    {
                                                        var streamTimeSpan = DateTime.Now - firstPCRTimeStampTime;
                                                        var dataTime = timeStamp.Value - firstPCRTimeStamp;
                                                        var shift = (streamTimeSpan).TotalSeconds - (dataTime);

                                                        if (shift < 0)
                                                        {
                                                            _timeShift = TimeSpan.MinValue;
                                                            firstPCRTimeStamp = ulong.MinValue;
                                                            firstPCRTimeStampTime = DateTime.MinValue;
                                                            continue;
                                                        }
                                                        //var speedCorrectionLShiftPerSec = shift / (streamTimeSpan).TotalSeconds;
                                                        var missingBytesForWholeStream = Math.Round((shift / loopsPerSecond) * bufferSize, 2);

                                                        PCR = $" (PCR time shift: {Math.Round(shift, 2).ToString("N2")} s, missingBytes: {GetHumanReadableSize(missingBytesForWholeStream)})";

                                                        var newBufferSize = GetCorrectedBufferSize(Convert.ToInt32(bufferSize + missingBytesForWholeStream));

                                                        if (newBufferSize > bufferSize)
                                                        {
                                                            if (newBufferSize > MaxBufferSize)
                                                            {
                                                                PCR += $" cannot increase buffer to {GetHumanReadableSize(newBufferSize)}";
                                                                newBufferSize = MaxBufferSize;
                                                            }

                                                            PCR += $" >>> {GetHumanReadableSize(newBufferSize)}";

                                                        }
                                                        else
                                                        if (newBufferSize < bufferSize)
                                                        {
                                                            if (newBufferSize < MinBufferSize)
                                                            {
                                                                PCR += $" cannot decrease buffer to {GetHumanReadableSize(newBufferSize)}";
                                                                newBufferSize = MinBufferSize;
                                                            }

                                                            PCR += $" <<< {GetHumanReadableSize(newBufferSize)}";
                                                        }

                                                        // calculate percentage change
                                                        var diff = newBufferSize - bufferSize;
                                                        var percChange = diff / (bufferSize / 100.0);

                                                        PCR += $" %%% change: {percChange.ToString("N2")}";

                                                        if (Math.Abs(percChange) > 25)
                                                        {
                                                            PCR += $"  TOO HIGH !!";
                                                            newBufferSize = bufferSize + Convert.ToInt32(Math.Sign(percChange) * 0.25 * bufferSize);
                                                        }

                                                        PCR += $" %%% change: {percChange.ToString("N2")}";

                                                        bufferSize = GetCorrectedBufferSize(Convert.ToInt32(newBufferSize));
                                                    }
                                                }
                                            }

                                            if ((DateTime.Now - lastSpeedCalculationTimeLog).TotalMilliseconds > 1000)
                                            {
                                                lastSpeedCalculationTimeLog = DateTime.Now;
                                                _loggingService.Debug($"TestingDVBTDriver sending data: {speed}, time for parse & send: {(DateTime.Now - lastSpeedCalculationTime).TotalMilliseconds} ms, {PCR}");
                                            }
                                        }
                                        else
                                        {
                                            _sendingDataPosition = 0;
                                            _timeShift = TimeSpan.MinValue;
                                            firstPCRTimeStamp = ulong.MinValue;
                                            firstPCRTimeStampTime = DateTime.MinValue;
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

                if (res.Length > 0 && _deliverySystem == 1)
                {
                    if (!_freqStreams.ContainsKey(_frequency))
                    {
                        _freqStreams.Add(_frequency, null);
                    }

                    _freqStreams[_frequency] = LoadBytesFromFile(res[0]);

                    _sendingDataPosition = 0;
                    _sendingDataFrequency = _frequency;
                    _timeShift = TimeSpan.MinValue;
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
