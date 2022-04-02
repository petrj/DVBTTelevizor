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
        private long LastFreq { get; set; }
        private long LastPID { get; set; }

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
            LastPID = 0;
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
            var eit = new EITManager(new BasicLoggingService());

            var timeAfterTenMinutes = DateTime.Now.AddMinutes(10);

            if (freq == 490000000)
            {
                eit.ScheduledEvents.Add(0, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 0,
                        EventName = "Advertisment - current program name",
                        StartTime = DateTime.Now.AddDays(-1),
                        FinishTime = timeAfterTenMinutes
                    },
                    new EventItem()
                {
                    EventId = 1,
                    EventName = "Advertisment - next program name",
                    StartTime = timeAfterTenMinutes,
                    FinishTime = DateTime.Now.AddHours(2)
                }
                });

                eit.ProgramNumberToMapPID.Add(0, 310);
            }

            if (freq == 514000000)
            {
                eit.ScheduledEvents.Add(0, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 0,
                        EventName = "Sport - current program name",
                        StartTime = DateTime.Now.AddDays(-1),
                        FinishTime = timeAfterTenMinutes
                    },
                    new EventItem()
                    {
                        EventId = 1,
                        EventName = "Sport - next program name",
                        StartTime = timeAfterTenMinutes,
                        FinishTime = DateTime.Now.AddHours(2)
                    }
                });

                eit.ScheduledEvents.Add(1, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 2,
                        EventName = "News - current program name",
                        StartTime = DateTime.Now.AddDays(-1),
                        FinishTime = timeAfterTenMinutes
                    },
                    new EventItem()
                    {
                        EventId = 3,
                        EventName = "News - next program name",
                        StartTime = timeAfterTenMinutes,
                        FinishTime = DateTime.Now.AddHours(2)
                    },

                });

                eit.ScheduledEvents.Add(2, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 4,
                        EventName = "Radio - current program name",
                        StartTime = DateTime.Now.AddDays(-1),
                        FinishTime = timeAfterTenMinutes
                    },
                    new EventItem()
                    {
                        EventId = 5,
                        EventName = "Radio - next program name",
                        StartTime = timeAfterTenMinutes,
                        FinishTime = DateTime.Now.AddHours(2)
                    }
                });

                eit.ProgramNumberToMapPID.Add(0, 2400);
                eit.ProgramNumberToMapPID.Add(1, 2300);
                eit.ProgramNumberToMapPID.Add(2, 7070);
            }

            return eit;
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
            if (PIDs != null && PIDs.Count > 0)
            {
                LastPID = PIDs[0];
            }

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
            if ((LastFreq == 490000000) || (LastFreq == 514000000))
            {
                var serviceDescriptors = new Dictionary<ServiceDescriptor, long>();

                if (LastFreq == 490000000)
                {
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex A",
                        ServiceName = "Advertisment",
                        ServisType = (byte)DVBTServiceType.TV
                    }, 310);
                }

                if (LastFreq == 514000000)
                {
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex B",
                        ServiceName = "News",
                        ServisType = (byte)DVBTServiceType.TV
                    }, 2300);
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex B",
                        ServiceName = "Sport",
                        ServisType = (byte)DVBTServiceType.TV
                    }, 2400);
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex B",
                        ServiceName = "Radio",
                        ServisType = (byte)DVBTServiceType.Radio
                    }, 7070);
                }

                return new SearchMapPIDsResult()
                {
                    Result = SearchProgramResultEnum.OK,
                    ServiceDescriptors = serviceDescriptors
                };
            } else
            {
                return new SearchMapPIDsResult()
                {
                    Result = SearchProgramResultEnum.NoProgramFound
                };
            }
        }

        public async Task<SearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            var res = new Dictionary<long, List<long>>();

            if (MapPIDs.Contains(310))
            {
                res.Add(310, new List<long>() { 3310, 3311, 3312, 3316, 3318, 3317, 8000 });
            }

            if (MapPIDs.Contains(2300))
            {
                res.Add(2300, new List<long>() { 2130, 2310, 2320, 2323, 2350, 2360 });
            }

            if (MapPIDs.Contains(2400))
            {
                res.Add(2400, new List<long>() { 2130, 2410, 2420, 2422, 2423, 2450, 2460 });
            }

            if (MapPIDs.Contains(7070))
            {
                res.Add(7070, new List<long>() { 7072, 7076 });
            }

            if (res.Count > 0)
            {
                return new SearchAllPIDsResult()
                {
                    Result = SearchProgramResultEnum.OK,
                    PIDs = res
                };
            } else
            {
                return new SearchAllPIDsResult()
                {
                    Result = SearchProgramResultEnum.NoProgramFound,
                    PIDs = null
                };
            }
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

        public async Task<TuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem)
        {
            LastFreq = frequency;

            if (
                (
                    (frequency == 490000000) || (frequency == 514000000)
                ) &&
                (bandWidth == 8000000) &&
                (deliverySystem == 1)
                )
            {
                return new TuneResult()
                {
                    Result = SearchProgramResultEnum.OK,
                    SignalPercentStrength = 100
                };
            }
            else
            {
                return new TuneResult()
                {
                    Result = SearchProgramResultEnum.NoSignal,
                    SignalPercentStrength = 0
                };
            }
        }

        public Stream VideoStream
        {
            get
            {
                var streamName = "stream.ts";

                if (LastPID != 0)
                {
                    streamName = LastPID.ToString()+".ts";
                    var fName = Path.Combine(BaseViewModel.AndroidAppDirectory, streamName);
                    if (!File.Exists(fName))
                    {
                        streamName = "stream.ts";
                    }
                }

                string streamFileName = Path.Combine(BaseViewModel.AndroidAppDirectory, streamName);
                return new FileStream(streamFileName, FileMode.Open, FileAccess.Read);
            }
        }
    }
}
