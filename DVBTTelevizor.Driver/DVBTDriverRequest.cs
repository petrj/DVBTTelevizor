using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTDriverRequest
    {
        public DVBTDriverRequestTypeEnum DVBTDriverRequestType;
        public List<long> Payload = new List<long>();

        public int ResponseBytesExpectedCount { get; set; }

        public DVBTDriverRequest(DVBTDriverRequestTypeEnum requestType, List<long> payload, int responseBytesExpectedCount)
        {
            DVBTDriverRequestType = requestType;
            ResponseBytesExpectedCount = responseBytesExpectedCount;
            Payload = payload;
        }

        public void Send(NetworkStream stream)
        {
            stream.Write(new byte[] { (byte)DVBTDriverRequestType }, 0, 1 );
            stream.Flush();
            stream.Write(new byte[] { (byte)Payload.Count }, 0, 1 );
            stream.Flush();

            foreach (var payload in Payload)
            {
                stream.Write(DVBTDriverStatus.GetByteArrayFromBigEndianLong(payload), 0, 8);
                stream.Flush();
            }
        }
    }
}
