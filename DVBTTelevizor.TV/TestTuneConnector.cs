﻿using LoggerService;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public class TestTuneConnector : IDriverConnector
    {
        public DVBTDriverStateEnum State { get; private set; }

        private long _lastFreq { get; set; }
        private long _lastPID { get; set; }

        private bool _driverInstalled = true;

        public event EventHandler? StatusChanged = null;
        ILoggingService _log;

        //public bool DriverInstalled { get; set; } = false;

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

        public TestTuneConnector(ILoggingService loggingService)
        {
            _log = loggingService;
        }

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
                return _lastFreq;
            }
        }

        public DVBTDriverConfiguration Configuration { get; set; } = new DVBTDriverConfiguration();

        public bool Connected { get; set; } = false;
        public bool Installed { get; set; } = true;

        public bool Recording { get; set; } = false;

        public bool ReadingStream { get; set; } = false;

        public bool Streaming { get; set; } = false;

        public string StreamUrl { get; set; } = "udp://@localhost:9600";

        public long Bitrate { get; set; } = 4000000; // 4 Mb/s

        public bool DriverStreamDataAvailable { get; set; } = true;

        public string PublicDirectory { get; set; } = "";

        public string RecordFileName
        {
            get
            {
                return Path.Combine(PublicDirectory, $"stream-{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}.ts");
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
            Streaming = true;
        }

        public void StopStream()
        {
            Streaming = false;
        }

        public async Task<bool> Stop()
        {
            _lastPID = 0;
            return true;
        }

        public async Task Disconnect()
        {
            Connected = false;
        }

        public async Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return new DVBTDriverCapabilities()
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

        public async Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000)
        {
        }

        public async Task<DVBTDriverSearchPIDsResult> SetupChannelPIDs(long mapPID, bool fastTuning)
        {
            var searchRes = await SearchProgramPIDs(mapPID, false);

            return new DVBTDriverSearchPIDsResult()
            {
                PIDs = searchRes.PIDs,
                Result = DVBTDriverSearchProgramResultEnum.OK
            };
        }

        public async Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            return true;
        }

        public async Task<DVBTDriverStatus> GetStatus()
        {
            return new DVBTDriverStatus()
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

        public async Task<DVBTDriverVersion> GetVersion()
        {
            return new DVBTDriverVersion()
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

            if (_lastFreq == 490000000)
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

            if (_lastFreq == 514000000)
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

            if (_lastFreq == 626000000)
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

        public async Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            if ((_lastFreq == 490000000) || (_lastFreq == 514000000) || (_lastFreq == 626000000))
            {
                var serviceDescriptors = new Dictionary<ServiceDescriptor, long>();

                if (_lastFreq == 490000000)
                {
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex A",
                        ServiceName = "Advertisment",
                        ServisType = (byte)DVBTDriverServiceType.TV
                    }, 310);
                }

                if (_lastFreq == 514000000)
                {
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex B",
                        ServiceName = "News",
                        ServisType = (byte)DVBTDriverServiceType.TV
                    }, 2300);
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex B",
                        ServiceName = "Sport",
                        ServisType = (byte)DVBTDriverServiceType.TV
                    }, 2400);
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex B",
                        ServiceName = "Radio",
                        ServisType = (byte)DVBTDriverServiceType.Radio
                    }, 7070);
                }


                if (_lastFreq == 626000000)
                {
                    serviceDescriptors.Add(new ServiceDescriptor()
                    {
                        ProviderName = "Multiplex C",
                        ServiceName = "INFO CHANNEL",
                        ServisType = (byte)DVBTDriverServiceType.TV
                    }, 8888);

                    for (var i=8889; i<= 8899; i++)
                    {
                        serviceDescriptors.Add(new ServiceDescriptor()
                        {
                            ProviderName = "Multiplex C",
                            ServiceName = "CHANNEL " + (i-8888).ToString(),
                            ServisType = (byte)DVBTDriverServiceType.TV
                        }, i);
                    }
                }

                return new DVBTDriverSearchProgramMapPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK,
                    ServiceDescriptors = serviceDescriptors
                };
            } else
            {
                return new DVBTDriverSearchProgramMapPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.NoProgramFound
                };
            }
        }

        public async Task<DVBTDriverSearchPIDsResult> SearchProgramPIDs(long mapPID, bool setPIDsAndSync)
        {
            var searchRes = await SearchProgramPIDs(new List<long> { mapPID });
            if (searchRes.PIDs == null || !searchRes.PIDs.ContainsKey(mapPID))
                    return new DVBTDriverSearchPIDsResult();

            return new DVBTDriverSearchPIDsResult()
            {
                Result = DVBTDriverSearchProgramResultEnum.OK,
                PIDs = searchRes.PIDs[mapPID]
            };
        }

        public async Task<DVBTDriverSearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
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
                return new DVBTDriverSearchAllPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK,
                    PIDs = res
                };
            } else
            {
                return new DVBTDriverSearchAllPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.NoProgramFound,
                    PIDs = null
                };
            }
        }

        public async Task<DVBTDriverResponse> SetPIDs(List<long> PIDs)
        {
            return new DVBTDriverResponse()
            {
                  SuccessFlag = true,
                  RequestTime = DateTime.Now,
                  ResponseTime = DateTime.Now,
                  Bytes  = new List<byte>()
            };
        }

        public async Task<DVBTDriverResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            return new DVBTDriverResponse()
            {
                SuccessFlag = true,
                RequestTime = DateTime.Now,
                ResponseTime = DateTime.Now,
                Bytes = new List<byte>()
            };
        }

        public async Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            _lastFreq = frequency;

            System.Threading.Thread.Sleep(fastTuning ? 100 : 1000);

            var res = new DVBTDriverTuneResult();

            if (
                (
                    (frequency == 490000000) || (frequency == 514000000) || (frequency == 626000000)
                ) &&
                (bandWidth == 8000000) &&
                (deliverySystem == 1)
                )
            {
                res.Result = DVBTDriverSearchProgramResultEnum.OK;
                res.SignalState.rfStrengthPercentage = 100;
            }
            else
            {
                res.Result = DVBTDriverSearchProgramResultEnum.NoSignal;
                res.SignalState.rfStrengthPercentage = 0;
            }

            return res;
        }

        public async Task<DVBTDriverTuneResult> WaitForSignal(bool fastTuning)
        {
            return new DVBTDriverTuneResult()
            {
                Result = DVBTDriverSearchProgramResultEnum.OK
            };
        }

        public async Task CheckPIDs()
        {
            await Task.Delay(100);
        }

        public Stream VideoStream
        {
            get
            {
                var streamName = "stream.ts";

                if (_lastPID != 0)
                {
                    streamName = _lastPID.ToString()+".ts";
                    var fName = Path.Combine(PublicDirectory, streamName);
                    if (!File.Exists(fName))
                    {
                        streamName = "stream.ts";
                    }
                }

                string streamFileName = Path.Combine(PublicDirectory, streamName);
                return new FileStream(streamFileName, FileMode.Open, FileAccess.Read);
            }
        }
    }
}
