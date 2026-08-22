using LoggerService;
using MPEGTS;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.TV
{
    public class RemoteVLCDriverConnector : IDriverConnector
    {
        private DVBTDriverConfiguration _driverConfiguration;
        private ILoggingService _log;
        private UDPStreamer _UDPStreamer;
        private long _lastTunedFreq = -1;
        private static object _readThreadLock = new object();
        private static object _infoLock = new object();
        private bool _readingStream = true;
        private bool _streaming = false;
        private bool _recording = false;
        private bool _readingBuffer = false;
        private string _dataStreamInfo = "Data reading not initialized";
        private string _IP = "127.0.0.1";
        private int _port = 1234;
        private string _password = "1234";


        public event EventHandler? OnRawAudioDemodulated;
        public event EventHandler? OnServiceFound;
        public event EventHandler? RawDataReceived;
        public event EventHandler StatusChanged;


        public RemoteVLCDriverConnector(ILoggingService loggingService, string IP, int port, string password)
        {
            _log = loggingService;

            _IP = IP;
            _port = port;
            _password = password;

            _log.Debug($"Initializing remote VLC driver");

            _UDPStreamer = new UDPStreamer(_log);
            _driverConfiguration = new DVBTDriverConfiguration();
        }


        public AppDriverTypeEnum DriverType
        {
            get
            {
                return AppDriverTypeEnum.DVBT;
            }
        }

        public DVBTDriverStateEnum State { get; private set; } = DVBTDriverStateEnum.Unknown;


        public bool Connected => throw new NotImplementedException();

        public DriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DriverStreamTypeEnum.UDP;
            }
        }

        public Stream VideoStream
        {
            get
            {
                return null;
            }
        }

        public string StreamUrl
        {
            get
            {
                if (_UDPStreamer == null)
                {
                    return "udp://@localhost:1234";
                }

                return $"udp://@{_UDPStreamer.IP}:{_UDPStreamer.Port}";
            }
        }

        public int QueueSize
        {
            get
            {
                return 0;
            }
        }

        public bool Synced
        {
            get
            {
                return DriverStreamDataAvailable;
            }
        }

        public bool Recording
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _recording;
                }
            }
        }

        public bool ReadingStream
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _readingStream;
                }
            }
        }

        public bool Streaming
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _streaming;
                }
            }
        }

        public string RecordFileName => throw new NotImplementedException();

        public string PublicDirectory { get; set; } = "";

        public string DataStreamInfo
        {
            get
            {
                lock (_infoLock)
                {
                    return _dataStreamInfo;
                }
            }
            set
            {
                lock (_infoLock)
                {
                    _dataStreamInfo = value;
                }
            }
        }

        public long Bitrate => throw new NotImplementedException();

        public long LastTunedFreq
        {
            get
            {
                return _lastTunedFreq;
            }
        }

        public bool DriverStreamDataAvailable => throw new NotImplementedException();


        public DVBTDriverConfiguration Configuration
        {
            get
            {
                return _driverConfiguration;
            }
            set
            {
                _driverConfiguration = value;
            }
        }

        public Task CheckPIDs()
        {
            throw new NotImplementedException();
        }

        public Task<bool> CheckStatus()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public Task Disconnect()
        {
            throw new NotImplementedException();
        }

        public Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverCapabilities> GetCapabalities()
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverStatus> GetStatus()
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverVersion> GetVersion()
        {
            throw new NotImplementedException();
        }

        public Task<EITScanResult> ScanEPG(int msTimeout = 2000)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverSearchPIDsResult> SearchProgramPIDs(long mapPID, bool setPIDsAndSync)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverSearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            throw new NotImplementedException();
        }

        public Task SetGain(GainEnum gain, int value = 0)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverResponse> SetPIDs(List<long> PIDs)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverSearchPIDsResult> SetupChannelPIDs(long mapPID, bool fastTuning)
        {
            throw new NotImplementedException();
        }

        public void StartRecording(string path)
        {
            throw new NotImplementedException();
        }

        public void StartStream()
        {
            throw new NotImplementedException();
        }

        public Task<bool> Stop()
        {
            throw new NotImplementedException();
        }

        public string StopRecording()
        {
            throw new NotImplementedException();
        }

        public void StopStream()
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            throw new NotImplementedException();
        }

        public Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000)
        {
            throw new NotImplementedException();
        }

        public Task<DVBTDriverTuneResult> WaitForSignal(bool fastTuning)
        {
            throw new NotImplementedException();
        }
    }
}
