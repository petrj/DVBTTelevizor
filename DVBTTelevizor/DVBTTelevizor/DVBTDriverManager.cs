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

namespace DVBTTelevizor
{
    public class DVBTDriverManager
    {
        DVBTTelevizorConfiguration _configuration;
        TcpClient _client;
        NetworkStream _nwStream;
        private ConnectionState _state = ConnectionState.Disconnected;

        BackgroundWorker _worker;

        object _requestLock;
        DVBTRequest _request;

        List<byte> _bytesBuffer = new List<byte>();
        
        public DVBTDriverManager()
        {
            Configuration = new DVBTTelevizorConfiguration();
            _worker = new BackgroundWorker();
            _worker.DoWork += worker_DoWork;

        }

        public bool Busy
        {
            get
            {
                return _worker.IsBusy;
            }
        }

        public void SetRequest(DVBTRequest request)
        {
            Request = request;
        }

        private DVBTRequest Request
        {
            get
            {
                lock (_requestLock)
                {
                    return _request;
                }
            }
            set
            {
                lock (_requestLock)
                {
                    _request = value;
                }
            }
        }

        public void Start()
        {
            _worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var client = new TcpClient();
            client.Connect("127.0.0.1", _configuration.Driver.ControlPort);

            _state = ConnectionState.Ready;
            //var stream = _client.GetStream();

            StreamReader sr = new StreamReader(client.GetStream());
            StreamWriter sw = new StreamWriter(client.GetStream());

            do
            {

                if (_state == ConnectionState.Ready)
                {
                    if (Request != null)
                    {
                        sw.Write(Request.BytesAsCharArray, 0, Request.Bytes.Count);
                        _state = ConnectionState.Busy;
                    }
                }

                char[] buffer = new char[client.ReceiveBufferSize];
                var readByteCount = sr.Read(buffer, 0, client.ReceiveBufferSize);
                if (readByteCount > 0)
                {
                    // adding bytes to uotput
                }

                System.Threading.Thread.Sleep(200);

            } while (_client.Connected);
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

        public void Connect()
        {
            _client = new TcpClient();
            _client.Connect("127.0.0.1", _configuration.Driver.ControlPort);

            /*
            //_client.Client.NoDelay = true;

            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            int size = sizeof(UInt32);
            UInt32 on = 1;
            UInt32 keepAliveInterval = 1000; //Send a packet once every second.
            UInt32 retryInterval = 1000; //If no response, resend every second.
            byte[] inArray = new byte[size * 3];
            Array.Copy(BitConverter.GetBytes(on), 0, inArray, 0, size);
            Array.Copy(BitConverter.GetBytes(keepAliveInterval), 0, inArray, size, size);
            Array.Copy(BitConverter.GetBytes(retryInterval), 0, inArray, size * 2, size);
            _client.Client.IOControl(IOControlCode.KeepAliveValues, inArray, null);

            _client.SendTimeout = 60 * 1000; // 1 min
            _client.ReceiveTimeout = 60 * 1000;
            */

            _nwStream = _client.GetStream();
        }

        private byte[] Send(byte[] bytesToSend, int responseSize, int secondsTimeout = 10)
        {
            _nwStream.Write(bytesToSend.ToArray(), 0, bytesToSend.Length);

            //_client.Client.Send(bytesToSend, bytesToSend.Length, SocketFlags.None);

            List<byte> bytesRead = new List<byte>();

            int totalBytesRead = 0;

            var startTime = DateTime.Now;

            //* byte 0 will be the Request.ordinal of the Request
            //* byte 1 will be N the number of longs in the payload
            //* byte 3 to byte 6 will be the success flag (0 or 1). This indicates whether the request was succesful.
            //* byte 7 till the end the rest of the longs in the payload follow
            //*  *
            //* Basically the success flag is always part of the payload, so the payload
            //* always consists of at least one value.

            /*
            byte[] bytesToReadPart = new byte[responseSize];
            _client.Client.Receive(bytesToReadPart, responseSize, SocketFlags.None);

            if (bytesToSend[0] != bytesToReadPart[0])
            {
                throw new Exception("Bad response");
            }
            */
            
            do
            {
                byte[] bytesToReadPart = new byte[responseSize];
                var bytes = _nwStream.Read(bytesToReadPart, 0, responseSize - totalBytesRead);
                totalBytesRead += bytes;
                for (var i = 0; i < bytes; i++) bytesRead.Add(bytesToReadPart[i]);

                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                System.Threading.Thread.Sleep(500);
                if (Math.Abs((DateTime.Now - startTime).TotalSeconds) > secondsTimeout)
                {
                    break;
                }
            }
            while (totalBytesRead < responseSize);            

            //for (var i = 0; i < bytesRead.Count; i++)
            //{
            //    Debug.WriteLine($"{i}: {bytesRead[i]}");
            //}

            return bytesRead.ToArray();
        }

        public DVBTStatus GetStatus()
        {
            var status = new DVBTStatus();

            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(3); // REQ_GET_STATUS
            bytesToSend.Add(0); // no payload
            

            var responseSize = 2 + 9 * 8;

            var bytesRead = Send(bytesToSend.ToArray(), responseSize);

            if (bytesRead.Length < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {bytesRead.Length }");

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];

            status.ParseFromByteArray(bytesRead.ToArray(), 2);

            return status;
        }

        public DVBTVersion GetVersion()
        {
            var version = new DVBTVersion();

            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(0); // REQ_PROTOCOL_VERSION
            bytesToSend.Add(0); // Payload size

            var bytesRead = Send(bytesToSend.ToArray(), 26);

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];

            version.SuccessFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 2) == 1;
            version.Version = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 10);
            version.AllRequestsLength = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 18);

            return version;
        }

        public DVBTResponse Tune(long frequency, long bandwidth, int deliverySyetem)
        {
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(2); // REQ_TUNE
            bytesToSend.Add(3); // Payload for 3 longs

            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(frequency)); // Payload[0] => frequency
            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(bandwidth)); // Payload[1] => bandWidth
            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(deliverySyetem));         // Payload[2] => DeliverySystem DVBT

            var responseSize = 10;

            var bytesRead = Send(bytesToSend.ToArray(), responseSize);

            if (bytesRead.Length < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {bytesRead.Length }");

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead.ToArray(), 2);

            return new DVBTResponse() { SuccessFlag = successFlag == 1 };
        }

        public DVBTResponse SendCloseConnection()
        {
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(1); // REQ_EXIT
            bytesToSend.Add(0); // REQ_EXIT

            var bytesRead = Send(bytesToSend.ToArray(), 10);

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 2);

            return new DVBTResponse() { SuccessFlag = successFlag == 1 };
        }

        public DVBTResponse SetPIDs(List<long> PIDs)
        {
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(4); // REQ_SET_PIDS
            bytesToSend.Add(Convert.ToByte(PIDs.Count));

            foreach (var pid in PIDs)
            {
                bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(pid));
            }

            var bytesRead = Send(bytesToSend.ToArray(), 10);

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead.ToArray(), 2);

            return new DVBTResponse() { SuccessFlag = successFlag == 1 };
        }
    }
}
