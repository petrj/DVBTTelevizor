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

        public DVBTDriverManager()
        {
            Configuration = new DVBTTelevizorConfiguration();
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
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _client.SendTimeout = 5000;
            _client.ReceiveTimeout = 50000;
            _nwStream = _client.GetStream();
        }

        private byte[] Send(byte[] bytesToSend, int responseSize, int secondsTimeout = 10)
        {
            _nwStream.Write(bytesToSend.ToArray(), 0, bytesToSend.Length);

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

            version.SuccessFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 2);
            version.Version = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 10);
            version.AllRequestsLength = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 18);

            return version;
        }

        public bool Tune(long frequency, long bandwidth, int deliverySyetem)
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

            return successFlag == 1;
        }

        public bool SendCloseConnection()
        {
            List<byte> bytesToSend = new List<byte>();

            bytesToSend.Add(1); // REQ_EXIT
            bytesToSend.Add(0); // REQ_EXIT

            var bytesRead = Send(bytesToSend.ToArray(), 10);

            var requestNumber = bytesRead[0];
            var longsCountInResponse = bytesRead[1];
            var successFlag = DVBTStatus.GetBigEndianLongFromByteArray(bytesRead, 2);

            return successFlag == 1;
        }

        public bool SetPIDs(List<long> PIDs)
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

            return successFlag == 1;
        }
    }
}
