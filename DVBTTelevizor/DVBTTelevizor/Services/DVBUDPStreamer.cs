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
        private int _port = 1234;

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

        public DVBUDPStreamer(ILoggingService log, string ip = null, int port = -1)
        {
            _log = log;

            if (ip == null)
            {
                _ip = "127.0.0.1";
            }

            if (port == -1)
            {
                if (DVBUDPStreamer.IsPortAvailable(1234)) // default VLC port for UDP stream (https://en.wikipedia.org/wiki/List_of_TCP_and_UDP_port_numbers)
                {
                    port = 1234;
                } else
                if (DVBUDPStreamer.IsPortAvailable(8000))
                {
                    port = 8000;
                }
                else
                {
                    port = DVBUDPStreamer.FindAvailablePort(32000, 33000);
                }

                if (port == -1)
                {
                    _log.Info($"UDPStreamer: No available port found, binding port 55555");
                    port = 55555;
                }

                _port = port;
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

        public static int FindAvailablePort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                {
                    return port;
                }
            }
            return -1; // No available port found
        }

        public static bool IsPortAvailable(int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                {
                    listener.Start();
                    listener.Stop();
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
