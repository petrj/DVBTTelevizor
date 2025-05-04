using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public class RTLSDRTCPIPFMDriverConnector : IDriverConnector
    {
        private ILoggingService _log;
        private RTLSDRDriver _driver = null;

        public RTLSDRTCPIPFMDriverConnector(ILoggingService loggingService)
        {
            _log = loggingService;

            _log.Debug($"Initializing RTLSDR TCP-IP FM Driver Connector");

            //_UDPStreamer = new UDPStreamer(_log);
            _driverConfiguration = new DVBTDriverConfiguration();

            _driver = new RTLSDRDriver(_log);
        }

        public DVBTDriverStateEnum State { get; private set; } = DVBTDriverStateEnum.Unknown;

        private DVBTDriverConfiguration _driverConfiguration;
        private bool _driverStreamDataAvailable = false;
        private string? _recordingFileName = null;

        private bool _driverInstalled = false;

        private bool _readingStream = true;
        private bool _streaming = false;
        private bool _recording = false;

        private static object _readThreadLock = new object();

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

        public string RecordFileName
        {
            get
            {
                lock (_readThreadLock)
                {
                    return _recordingFileName;
                }
            }
        }


        public bool DriverStreamDataAvailable
        {
            get
            {
                return _driverStreamDataAvailable;
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

        public bool Connected
        {
            get
            {
                return false;
            }
        }

        public bool DriverInstalled
        {
            get
            {
                return _driverInstalled;
            }
            set
            {
                _driverInstalled = value;
            }
        }

        public DVBTDriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DVBTDriverStreamTypeEnum.Stream;
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
                return "udp://@localhost:1234";
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

        public string PublicDirectory { get; set; }

        public string DataStreamInfo { get; set; }

        public long Bitrate { get; set; }

        public long LastTunedFreq { get; set; }

        public event EventHandler StatusChanged;

        public Task CheckPIDs()
        {
            return Task.Run(() => { return; });
        }

        public Task<bool> CheckStatus()
        {
            return Task.Run(() => { return false; });
        }

        public void Connect()
        {
        }

        public Task Disconnect()
        {
            return Task.Run(() => { return; });
        }

        public Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            return Task.Run(() => { return false; });
        }

        public Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() => { return new DVBTDriverCapabilities(); });
        }

        public Task<DVBTDriverStatus> GetStatus()
        {
            return Task.Run(() => { return new DVBTDriverStatus(); });
        }

        public Task<DVBTDriverVersion> GetVersion()
        {
            return Task.Run(() => { return new DVBTDriverVersion(); });
        }

        public Task<EITScanResult> ScanEPG(int msTimeout = 2000)
        {
            return Task.Run(() => { return new EITScanResult(); });
        }

        public Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return Task.Run(() => { return new DVBTDriverSearchProgramMapPIDsResult(); });
        }

        public Task<DVBTDriverSearchPIDsResult> SearchProgramPIDs(long mapPID, bool setPIDsAndSync)
        {
            return Task.Run(() => { return new DVBTDriverSearchPIDsResult(); });
        }

        public Task<DVBTDriverSearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            return Task.Run(() => { return new DVBTDriverSearchAllPIDsResult(); });
        }

        public Task<DVBTDriverResponse> SetPIDs(List<long> PIDs)
        {
            return Task.Run(() => { return new DVBTDriverResponse(); });
        }

        public Task<DVBTDriverSearchPIDsResult> SetupChannelPIDs(long mapPID, bool fastTuning)
        {
            return Task.Run(() => { return new DVBTDriverSearchPIDsResult(); });
        }

        public Task StartRecording()
        {
            return Task.Run(() => { return; });
        }

        public void StartStream()
        {
        }

        public Task<bool> Stop()
        {
            return Task.Run(() => { return false; });
        }

        public void StopRecording()
        {
        }

        public void StopStream()
        {
        }

        public Task<DVBTDriverResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            return Task.Run(() => { return new DVBTDriverResponse(); });
        }

        public Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            return Task.Run(() => { return new DVBTDriverTuneResult(); });
        }

        public Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000)
        {
            return Task.Run(() => { return; });
        }

        public Task<DVBTDriverTuneResult> WaitForSignal(bool fastTuning)
        {
            return Task.Run( () => { return new DVBTDriverTuneResult();  } );
        }
    }
}
