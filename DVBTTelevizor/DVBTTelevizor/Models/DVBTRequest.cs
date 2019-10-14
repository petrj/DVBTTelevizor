using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace DVBTTelevizor
{
    public class DVBTRequest
    {
        public DVBTDriverRequestTypeEnum DVBTDriverRequestType;
        public List<long> Payload = new List<long>();

        //public List<byte> Bytes { get; set; } = new List<byte>();
        public int ResponseBytesExpectedCount { get; set; }

        /*
        public char[] BytesAsCharArray
        {
            get
            {
                if (Bytes.Count>0)
                {
                    var res = new char[Bytes.Count];
                    for (var i = 0; i < Bytes.Count; i++) res[i] = Convert.ToChar(Bytes[i]);
                    return res;
                } else
                {
                    return new char[0];
                }
            }
        }
        */

        public DVBTRequest(DVBTDriverRequestTypeEnum requestType, List<long> payload, int responseBytesExpectedCount)
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
                stream.Write(DVBTStatus.GetByteArrayFromBigEndianLong(payload), 0, 8);
                stream.Flush();
            }
        }
    }
}
