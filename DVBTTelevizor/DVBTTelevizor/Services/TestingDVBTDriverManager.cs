using DVBTTelevizor.Models;
using LoggerService;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public class TestingDVBTDriverManager : IDVBTDriverManager
    {
        public DVBTDriverConfiguration Configuration { get; set; } = new DVBTDriverConfiguration();

        public bool Started { get; set; } = false;

        public bool Recording { get; set; } = false;

        public bool ReadingStream { get; set; } = false;

        public string RecordFileName { get; set; } = null;

        public string DataStreamInfo { get; set; } = "Data reading not initialized";

        public List<byte> Buffer { get; set; } = new List<byte>();

        public async Task<bool> CheckStatus()
        {
            return Started;
        }

        public void Start()
        {
            Started = true;
        }

        public async Task StartRecording()
        {

        }

        public async Task<bool> Stop()
        {
            return true;
        }

        public void StopReadStream()
        {

        }

        public void StopRecording()
        {

        }

        public async Task Disconnect()
        {
            Started = false;
        }

        public async Task<DVBTCapabilities> GetCapabalities()
        {
            return new DVBTCapabilities()
            {
                supportedDeliverySystems = 3,
                minFrequency = 0,
                maxFrequency = 0,
                frequencyStepSize = 8,
                vendorId = 0,
                productId = 0
            };
        }

        public EITManager GetEITManager(long freq)
        {
            return new EITManager(new BasicLoggingService())
            {

            };
        }

        public async Task<DVBTStatus> GetStatus()
        {
            return new DVBTStatus()
            {
                snr = 0,
                bitErrorRate  = 0,
                droppedUsbFps  = 0,
                rfStrengthPercentage  = 100,
                hasSignal  = 1,
                hasCarrier = 1,
                hasSync  = 1,
                hasLock  = 1
            };
        }

        public async Task<DVBTVersion> GetVersion()
        {
            return new DVBTVersion()
            {
                Version = 1,
                AllRequestsLength = 0
            };
        }

        public async Task<PlayResult> Play(long frequency, long bandwidth, int deliverySystem, List<long> PIDs, bool stopReadStream = true)
        {
            return new PlayResult()
            {
                 OK = true,
                 SignalStrengthPercentage = 100
            };
        }

        public async Task<EITScanResult> ScanEPG(long freq, int msTimeout = 2000)
        {
            return new EITScanResult()
            {
                OK = true,
                UnsupportedEncoding = false
            };
        }

        public async Task<EITScanResult> ScanEPGForChannel(long freq, int programMapPID, int msTimeout = 2000)
        {
            return new EITScanResult()
            {
                OK = true,
                UnsupportedEncoding = false
            };
        }

        public async Task<SearchMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return new SearchMapPIDsResult()
            {
                 Result = SearchProgramResultEnum.NoProgramFound,
                 ServiceDescriptors = null
            };
        }

        public async Task<SearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            return new SearchAllPIDsResult()
            {
                Result = SearchProgramResultEnum.NoProgramFound,
                PIDs = null
            };
        }

        public async Task<DVBTResponse> SetPIDs(List<long> PIDs)
        {
            return new DVBTResponse()
            {
                  SuccessFlag = true,
                  RequestTime = DateTime.Now,
                  ResponseTime = DateTime.Now,
                  Bytes  = new List<byte>()
            };
        }

        public async Task<DVBTResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            return new DVBTResponse()
            {
                SuccessFlag = true,
                RequestTime = DateTime.Now,
                ResponseTime = DateTime.Now,
                Bytes = new List<byte>()
            };
        }

        public async Task<TuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySyetem)
        {
            return new TuneResult()
            {
                SignalPercentStrength = 100
            };
        }

        public Stream VideoStream
        {
            get
            {
                string streamFileName = Path.Combine(BaseViewModel.AndroidAppDirectory, "stream.ts");

                return new FileStream(streamFileName, FileMode.Open, FileAccess.Read);
            }
        }
    }
}
