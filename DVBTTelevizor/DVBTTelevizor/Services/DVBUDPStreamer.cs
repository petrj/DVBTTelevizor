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

        private bool _active = false;
        private ILoggingService _log;
        private UdpClient _currentUDPClient = null;
        private IPEndPoint _currentEndPoint = null;

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
                return _currentUDPClient;
            }
        }

        public IPEndPoint CurrentEndPoint
        {
            get
            {
                return _currentEndPoint;
            }
        }

        public bool Active
        {
            get
            {
                return _active;
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
        }

        public void StartUDPClientThread()
        {
            _log.Info($"UDPStreamer: Starting UDP client thread");

            Task.Run(delegate
            {
                _active = true;
                _currentUDPClient = null;

                using (UdpClient udpClient = new UdpClient())
                {
                    // Set up the destination endpoint
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(IP), Port);

                    _currentUDPClient = udpClient;
                    _currentEndPoint = endPoint;

                    while (_active)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }

                _log.Info($"UDPStreamer: UDP client thread finished");
            });
        }

        public void SendByteArray(byte[] array, int count)
        {
            if (_currentUDPClient == null || _currentEndPoint == null)
                return;

            if (array != null && count > 0)
            {
                _log.Info($"[UDP] --> {(count / 1024).ToString("N0")} KB");

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
                    _currentUDPClient.Send(bufferPart, bufferPartSize, _currentEndPoint);
                    bufferPos += bufferPartSize;
                }
            }
        }

        public void SendByteArray(List<byte> bytes)
        {
            if (_currentUDPClient == null || _currentEndPoint == null)
                return;

            if (bytes != null && bytes.Count > 0)
            {
                _log.Info($"[UDP] --> {(bytes.Count/1024).ToString("N0")} KB");

                var bufferPart = new byte[MaxPacketSize];
                var bufferPos = 0;

                while (bufferPos < bytes.Count)
                {
                    if (bufferPos + MaxPacketSize <= bytes.Count)
                    {
                        bytes.CopyTo(bufferPos, bufferPart, 0, MaxPacketSize);
                        _currentUDPClient.Send(bufferPart, MaxPacketSize, _currentEndPoint);
                        bufferPos += MaxPacketSize;
                    }
                    else
                    {
                        bytes.CopyTo(bufferPos, bufferPart, 0, bytes.Count - bufferPos);
                        _currentUDPClient.Send(bufferPart, bytes.Count - bufferPos, _currentEndPoint);
                        bufferPos += bytes.Count - bufferPos;
                    }
                }
            }
        }

        public void StopUDPClientThread()
        {
            _log.Info($"UDPStreamer: Stopping UDP client thread");

            _active = false;
            _currentUDPClient = null;
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
