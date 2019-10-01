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
        DVBTBackgroundRequest _request = new DVBTBackgroundRequest();

        BackgroundWorker _worker;

        private static object _workerLock = new object();

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

        public async Task<DVBTResponse> SendRequest(DVBTRequest request, int secondsTimeout = 10)
        {
            lock (_workerLock)
            {
                if (_request.State == RequestStateEnum.Error)
                {
                    _request.State = RequestStateEnum.Ready; 
                }                  

                if (_request.State != RequestStateEnum.Ready)
                    throw new Exception("Driver not ready");

                _request.Request = request;
                _request.State = RequestStateEnum.SendingRequest;
            }

            var startTime = DateTime.Now;

           return await Task.Run(() =>
           {
               try
               {
                   do
                   {
                       var timeSpan = Math.Abs((DateTime.Now - startTime).TotalSeconds);
                       if (timeSpan > secondsTimeout)
                       {
                           throw new TimeoutException("TimeOut");
                       }

                       System.Threading.Thread.Sleep(200);
                   }
                   while (_request.State != RequestStateEnum.ResponseReceived);

                   lock (_workerLock)
                   {
                       _request.State = RequestStateEnum.Ready;
                   }

               }
               catch (TimeoutException)
               {
                   lock (_workerLock)
                   {
                       _request.State = RequestStateEnum.Error;
                       return new DVBTResponse() { SuccessFlag = false };
                   }
               }

               return _request.Response;
           });
        }


        public void Start()
        {
            _worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var stayAliveConnectionRequestMiliseconds = 1000;
            var lastStayAliveConnectionRequestTime = DateTime.MinValue;

            var client = new TcpClient();
            client.Connect("127.0.0.1", _configuration.Driver.ControlPort);
            client.SendTimeout = int.MaxValue;
            client.ReceiveTimeout = int.MaxValue;

            var stream = client.GetStream();
            byte[] buffer = new byte[1024];
            bool readingAlllowed = false;

            bool stayAliveReading = false;
            List<byte> stayAliveBytes = new List<byte>();

            do
            {
            
                lock (_workerLock)
                {
                    if (!stayAliveReading && _request.State == RequestStateEnum.SendingRequest)
                    {
                        //client.Client.Send(_request.Request.Bytes.ToArray());
                        stream.Write(_request.Request.Bytes.ToArray(), 0, _request.Request.Bytes.Count);
                        readingAlllowed = true;

                        _request.Response = new DVBTResponse();
                        _request.State = RequestStateEnum.ReadingResponse;                        
                    }
                }

                
                if (readingAlllowed)
                {
                    var readByteCount = stream.Read(buffer, 0, 1024);
                    if (readByteCount > 0)
                    {
                        lock (_workerLock)
                        {
                            // adding bytes to output
                            if (_request.State == RequestStateEnum.ReadingResponse)
                            {                              
                                for (var i = 0; i < readByteCount; i++)
                                {
                                    _request.Response.Bytes.Add(buffer[i]);
                                }
                                if (_request.Request.ResponseBytesExpectedCount == _request.Response.Bytes.Count)
                                {
                                    _request.State = RequestStateEnum.ResponseReceived;
                                    readingAlllowed = false;
                                }                                
                            }
                        }
                    }                   
                }     

                System.Threading.Thread.Sleep(500);

                if (!readingAlllowed && !stayAliveReading)
                {
                    // stay alive connection request- sending get version every x miliseconds
                    if ((DateTime.Now - lastStayAliveConnectionRequestTime).TotalMilliseconds > stayAliveConnectionRequestMiliseconds)
                    {
                        stream.Write(new byte[] { 0, 0 }, 0, 2);
                        stayAliveBytes.Clear();
                        stayAliveReading = true;
                    }                   
                }

                if (stayAliveReading)
                {
                    var bytesread = stream.Read(buffer, 0, 26);
                    if (bytesread > 0)
                    {
                        for (var i = 0; i < bytesread; i++)
                        {
                            stayAliveBytes.Add(buffer[i]);
                        }
                        
                        if (stayAliveBytes.Count == 26)
                        {
                            stayAliveReading = false;
                        }
                    }
                }

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

            var response = await SendRequest(req, 5);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];

            status.ParseFromByteArray(response.Bytes.ToArray(), 2);

            return status;
        }

        public async Task<DVBTVersion> GetVersion()
        {
            var version = new DVBTVersion();
            
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(0); // REQ_PROTOCOL_VERSION
            bytesToSend.Add(0); // Payload size

            var responseSize = 26;  // 1 + 1 + 8 + 8 + 8

            var req = new DVBTRequest(bytesToSend, responseSize);
            var response = await SendRequest(req, 5);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];

            var ar = response.Bytes.ToArray();

            version.SuccessFlag = DVBTStatus.GetBigEndianLongFromByteArray(ar, 2) == 1;
            version.Version = DVBTStatus.GetBigEndianLongFromByteArray(ar, 10);
            version.AllRequestsLength = DVBTStatus.GetBigEndianLongFromByteArray(ar, 18);
            
            return version;
        }

        public async Task<DVBTResponse> Tune(long frequency, long bandwidth, int deliverySyetem)
        {
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(2); // REQ_TUNE
            bytesToSend.Add(3); // Payload for 3 longs

            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(frequency)); // Payload[0] => frequency
            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(bandwidth)); // Payload[1] => bandWidth
            bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(deliverySyetem));         // Payload[2] => DeliverySystem DVBT

            var responseSize = 10;

            var req = new DVBTRequest(bytesToSend, responseSize);
            var response = await SendRequest(req, 10);

            if (response.Bytes.Count < responseSize)
                throw new Exception($"Bad response, expected {responseSize} bytes, received {response.Bytes.Count  }");

            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            return new DVBTResponse() { SuccessFlag = successFlag == 1 };
        }

        public async Task<DVBTResponse> SendCloseConnection()
        {            
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(1); // REQ_EXIT
            bytesToSend.Add(0);

            var responseSize = 10;

            var req = new DVBTRequest(bytesToSend, responseSize);
            var response = await SendRequest(req, 5);

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            return new DVBTResponse() { SuccessFlag = successFlag == 1 };
        }

        public async Task<DVBTResponse> SetPIDs(List<long> PIDs)
        {        
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(4); // REQ_SET_PIDS
            bytesToSend.Add(Convert.ToByte(PIDs.Count));

            foreach (var pid in PIDs)
            {
                bytesToSend.AddRange(DVBTStatus.GetByteArrayFromBigEndianLong(pid));
            }

            var responseSize = 10;

            var req = new DVBTRequest(bytesToSend, responseSize);
            var response = await SendRequest(req, 5);

            var requestNumber = response.Bytes[0];
            var longsCountInResponse = response.Bytes[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(response.Bytes.ToArray(), 2);

            return new DVBTResponse() { SuccessFlag = successFlag == 1 };
        }
    }
}
