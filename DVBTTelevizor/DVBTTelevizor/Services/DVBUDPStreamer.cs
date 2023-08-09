using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using LoggerService;
using System.Threading.Tasks;

namespace DVBTTelevizor.Services
{
    public class DVBUDPStreamer
    {
        public const int MaxPacketSize = 1400;

        private ILoggingService _log;
        private UdpClient _UDPClient = null;
        private IPEndPoint _EndPoint = null;

        private string _ip;
        private int _port = 8001;

        public string IP
        {
            get
            {
                return _ip;
            }
        }

        public UdpClient CurrentUDPClient
        {
            get
            {
                return _UDPClient;
            }
        }

        public IPEndPoint CurrentEndPoint
        {
            get
            {
                return _EndPoint;
            }
        }

        public int Port
        {
            get
            {
                return _port;
            }
        }

        public DVBUDPStreamer(ILoggingService log, string ip = null)
        {
            _log = log;

            if (ip == null)
            {
                var ipAddr = DVBUDPStreamer.GetLocalIPAddress();
                if (ipAddr == null)
                {
                    _ip = "localhost";
                } else
                {
                    _ip = ipAddr.ToString();
                }
            }

            _log.Info($"UDPStreamer: Binding address {IP}:{Port}");

            _UDPClient = new UdpClient();
            _EndPoint = new IPEndPoint(IPAddress.Parse(IP), Port);
        }

        public void SendByteArray(byte[] array, int count)
        {
            try
            {
                if (_UDPClient == null || _EndPoint == null)
                    return;

                if (array != null && count > 0)
                {
                    //_log.Info($"[UDP] --> {(count / 1024).ToString("N0")} KB");

                    var bufferPart = new byte[MaxPacketSize];
                    var bufferPartSize = 0;
                    var bufferPos = 0;

                    while (bufferPos < count)
                    {
                        if (bufferPos + MaxPacketSize <= count)
                        {
                            bufferPartSize = MaxPacketSize;
                        }
                        else
                        {
                            bufferPartSize = count - bufferPos;
                        }

                        Buffer.BlockCopy(array, bufferPos, bufferPart, 0, bufferPartSize);
                        _UDPClient.Send(bufferPart, bufferPartSize, _EndPoint);
                        bufferPos += bufferPartSize;
                    }
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public static IPAddress GetLocalIPAddress()
        {
            string hostName = Dns.GetHostName();
            var hostEntry = Dns.GetHostEntry(hostName);
            foreach (var ipAddress in hostEntry.AddressList)
            {
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ipAddress;
                }
            }
            //throw new Exception("No suitable IP address found.");
            return null;
        }
    }
}
