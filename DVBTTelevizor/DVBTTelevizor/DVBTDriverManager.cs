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
        private DVBTDriverState _state = DVBTDriverState.Disconnected;

        BackgroundWorker _worker;

        private static object _workerLock = new object();

        DVBTRequest _request;
        DVBTResponse _response;

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

        public async Task<DVBTResponse> SendRequest(DVBTRequest request, int secondsTimeout = 100)
        {
            if (_state != DVBTDriverState.Ready)
                throw new Exception("Driver not ready");

            //lock (_workerLock)
            {
                _request = request;
                _state = DVBTDriverState.SendingRequest;
            }

            var startTime = DateTime.Now;

           await Task.Run(() =>
           {
                do
                {
                   var timeSpan = Math.Abs((DateTime.Now - startTime).TotalSeconds);
                   if (timeSpan > secondsTimeout)
                   {
                       throw new Exception("TimeOut");
                   }

                   System.Threading.Thread.Sleep(200);

               }
               while (_state != DVBTDriverState.ResponseReceived);
           });

            DVBTResponse res;

            //lock (_workerLock)
            {
                _state = DVBTDriverState.Ready;
            }

            return _response;
        }


        public void Start()
        {
            _worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var client = new TcpClient();
            client.Connect("127.0.0.1", _configuration.Driver.ControlPort);

            lock (_workerLock)
            {
                _state = DVBTDriverState.Ready;
            }

            //var sr = new StreamReader(client.GetStream());
            //var sw = new StreamWriter(client.GetStream());
            var stream = client.GetStream();

            do
            {
                if ((_state == DVBTDriverState.SendingRequest) && (_request != null))
                {
                    stream.Write(_request.Bytes.ToArray(), 0, _request.Bytes.Count);
                    //lock (_workerLock)
                    {
                        _response = new DVBTResponse();
                        _state = DVBTDriverState.ReadingResponse;
                    }
                }

                byte[] buffer = new byte[client.ReceiveBufferSize];
                var readByteCount = stream.Read(buffer, 0, client.ReceiveBufferSize);
                if (readByteCount > 0)
                {

                    // adding bytes to output
                    if (_state == DVBTDriverState.ReadingResponse)
                    {
                        //lock (_workerLock)
                        {
                            for (var i = 0; i < readByteCount; i++)
                            {
                                _response.Bytes.Add(Convert.ToByte(buffer[i]));
                            }
                            if (_request.ResponseBytesExpectedCount == _response.Bytes.Count)
                            {
                                _state = DVBTDriverState.ResponseReceived;
                            }
                        }
                    }
                }

                System.Threading.Thread.Sleep(200);

            } while (client.Connected);
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

            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(3); // REQ_GET_STATUS
            bytesToSend.Add(0); // no payload

            var responseSize = 2 + 9 * 8;

            var req = new DVBTRequest(bytesToSend, responseSize);

            var response = await SendRequest(req);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];

            status.ParseFromByteArray(response.Bytes.ToArray(), 2);

            return status;
        }

        public DVBTVersion GetVersion()
        {
            var version = new DVBTVersion();
            /*
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(0); // REQ_PROTOCOL_VERSION
            bytesToSend.Add(0); // Payload size

            var bytesRead = Send(bytesToSend.ToArray(), 26);

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];

            version.SuccessFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 2) == 1;
            version.Version = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 10);
            version.AllRequestsLength = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 18);
            */
            return version;
        }

        public DVBTResponse Tune(long frequency, long bandwidth, int deliverySyetem)
        {
            /*
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

            return new DVBTResponse() { SuccessFlag = successFlag == 1 };*/
            return new DVBTResponse();
        }

        public DVBTResponse SendCloseConnection()
        {
            /*
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(1); // REQ_EXIT
            bytesToSend.Add(0); // REQ_EXIT

            var bytesRead = Send(bytesToSend.ToArray(), 10);

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 2);

            return new DVBTResponse() { SuccessFlag = successFlag == 1 };
            */
            return new DVBTResponse();
        }

        public DVBTResponse SetPIDs(List<long> PIDs)
        {
            /*
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
            */
            return new DVBTResponse();
        }
    }
}
