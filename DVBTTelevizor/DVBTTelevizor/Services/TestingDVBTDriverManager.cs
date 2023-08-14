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

        public DVBTDriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DVBTDriverStreamTypeEnum.Stream;
            }
        }

        public long LastTunedFreq
        {
            get
            {
                return LastFreq;
            }
        }

        public DVBTDriverConfiguration Configuration { get; set; } = new DVBTDriverConfiguration();

        public bool Connected { get; set; } = false;

        public bool Recording { get; set; } = false;

        public bool ReadingStream { get; set; } = false;

        public bool Streaming { get; set; } = true;

        public string StreamUrl { get; set; } = "udp://@localhost:9600";

        public bool DriverStreamDataAvailable { get; set; } = true;

        public string RecordFileName
        {
            get
            {
                return Path.Combine(BaseViewModel.AndroidAppDirectory, $"stream-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.ts");
            }
        }

        public string DataStreamInfo { get; set; } = "Data reading not initialized";

        public List<byte> Buffer { get; set; } = new List<byte>();

        public async Task<bool> CheckStatus()
        {
            return Connected;
        }

        public void Connect()
        {
            Connected = true;
        }

        public async Task StartRecording()
        {
            Recording = true;
        }

        public void StopRecording()
        {
            Recording = false;
        }

        public void StopReadStream()
        {
        }

        public void StartStream()
        {
        }

        public void StopStream()
        {
        }

        public async Task<bool> Stop()
        {
            LastPID = 0;
            return true;
        }

        public async Task Disconnect()
        {
            Connected = false;
        }

        public async Task<DVBTCapabilities> GetCapabalities()
        {
            return new DVBTCapabilities()
            {
                SuccessFlag = true,

                supportedDeliverySystems = 3,
                minFrequency = 474000000,
                maxFrequency = 714000000,
                frequencyStepSize = 8,
                vendorId = 0,
                productId = 0
            };
        }

        public async Task WaitForBufferPIDs(List<long> PIDs, int msTimeout = 3000)
        {
        }

        public async Task<DVBTStatus> GetStatus()
        {
            return new DVBTStatus()
            {
                SuccessFlag = true,

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
                SuccessFlag = true,

                Version = 1,
                AllRequestsLength = 0
            };
        }

        public async Task<EITScanResult> ScanEPG(int msTimeout = 2000)
        {
            var res = new EITScanResult();

            var timeAfterTenMinutes = DateTime.Now.AddMinutes(10);
            var timeBefore5Minutes = DateTime.Now.AddMinutes(-5);
            var timeBefore50Minutes = DateTime.Now.AddMinutes(-50);

            var loremIpsumText = "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe eveniet ut et voluptates repudiandae sint et molestiae non recusandae. Integer pellentesque quam vel velit. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Vivamus porttitor turpis ac leo. Praesent dapibus. Aliquam erat volutpat. Etiam commodo dui eget wisi. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos hymenaeos. Quisque porta. Phasellus faucibus molestie nisl. Aenean id metus id velit ullamcorper pulvinar. Suspendisse sagittis ultrices augue. In rutrum. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Integer tempor. Mauris metus. In laoreet, magna id viverra tincidunt, sem odio bibendum justo, vel imperdiet sapien wisi sed libero. In convallis. Fusce nibh. Integer in sapien. Aenean vel massa quis mauris vehicula lacinia. Aliquam erat volutpat. Fusce tellus. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem. Maecenas sollicitudin. Curabitur bibendum justo non orci. Maecenas libero. Nullam eget nisl. Nullam justo enim, consectetuer nec, ullamcorper ac, vestibulum in, elit. Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum qui dolorem eum fugiat quo voluptas nulla pariatur? Nullam sapien sem, ornare ac, nonummy non, lobortis a enim. Vestibulum erat nulla, ullamcorper nec, rutrum non, nonummy ac, erat. Curabitur vitae diam non enim vestibulum interdum. Aenean placerat. Pellentesque ipsum. Etiam dui sem, fermentum vitae, sagittis id, malesuada in, quam. Duis ante orci, molestie vitae vehicula venenatis, tincidunt ac pede. Mauris metus. Sed vel lectus.";

            if (LastFreq == 490000000)
            {
                res.CurrentEvents.Add(310,
                    new EventItem()
                    {
                        EventId = 0,
                        EventName = "Advertisment - current program name",
                        Text = $"Fructies, Garnier and other ... {Environment.NewLine}{Environment.NewLine}{loremIpsumText}",
                        StartTime = timeBefore5Minutes,
                        FinishTime = timeAfterTenMinutes
                    });

                res.ScheduledEvents.Add(310, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 0,
                        EventName = "Advertisment - current program name",
                        Text = $"Fructies, Garnier and other ... {Environment.NewLine}{Environment.NewLine}{loremIpsumText}",
                        StartTime = timeBefore5Minutes,
                        FinishTime = timeAfterTenMinutes
                    },
                    new EventItem()
                    {
                        EventId = 1,
                        EventName = "Advertisment - next program name",
                        StartTime = timeAfterTenMinutes,
                        FinishTime = DateTime.Now.AddHours(2)
                    },
                    new EventItem()
                    {
                        EventId = 2,
                        EventName = "Advertisment - another program name",
                        StartTime = DateTime.Now.AddHours(2),
                        FinishTime = DateTime.Now.AddHours(3)
                    }
                });
            }

            if (LastFreq == 514000000)
            {
                res.ScheduledEvents.Add(2400, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 0,
                        EventName = "Sport - current program name",
                        Text = $"Figure skating {Environment.NewLine}{Environment.NewLine}{loremIpsumText}",
                        StartTime = timeBefore5Minutes,
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

                res.ScheduledEvents.Add(2300, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 2,
                        EventName = "News - current program name",
                        Text = $"War map sitiation{Environment.NewLine}{Environment.NewLine}{loremIpsumText}",
                        StartTime = timeBefore50Minutes,
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

                res.ScheduledEvents.Add(7070, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 4,
                        EventName = "Radio - current program name",
                        Text = $"Audio book reading {Environment.NewLine}{Environment.NewLine}{loremIpsumText}",
                        StartTime = timeBefore5Minutes,
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
            }

            if (LastFreq == 626000000)
            {
                res.ScheduledEvents.Add(8888, new List<EventItem>()
                {
                    new EventItem()
                    {
                        EventId = 0,
                        EventName = "Informations",
                        StartTime = timeBefore5Minutes,
                        FinishTime = timeAfterTenMinutes
                    }
                });
            }

            res.OK = true;

            return res;
        }

        public async Task<SearchMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            if ((LastFreq == 490000000) || (LastFreq == 514000000) || (LastFreq == 626000000))
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


                if (LastFreq == 626000000)
                {
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex C",
                        ServiceName = "INFO CHANNEL",
                        ServisType = (byte)DVBTServiceType.TV
                    }, 8888);

                    for (var i=8889; i<= 8899; i++)
                    {
                        serviceDescriptors.Add(new ServiceDescriptor()
                        {
                            ProviderName = "Multiplex C",
                            ServiceName = "CHANNEL " + (i-8888).ToString(),
                            ServisType = (byte)DVBTServiceType.TV
                        }, i);
                    }
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

            if (MapPIDs.Contains(8888))
            {
                res.Add(8888, new List<long>() { 8889 });
            }

            for (var i = 8889; i <= 8899; i++)
            {
                if (MapPIDs.Contains(i))
                {
                    res.Add(i, new List<long>() { i });
                }
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

        public async Task<TuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, long mapPID, bool fastTuning)
        {
            return await TuneEnhanced(frequency, bandWidth, deliverySystem, new List<long>() { mapPID }, fastTuning);
        }

        public async Task<TuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, List<long> PIDs, bool fastTuning)
        {
            LastFreq = frequency;
            LastPID = PIDs[0];

            System.Threading.Thread.Sleep(fastTuning ? 100 : 1000);

            if (
                (
                    (frequency == 490000000) || (frequency == 514000000) || (frequency == 626000000)
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
