using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.FM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.TV
{
    public class RTLSDRDriverConnector : IDriverConnector
    {
        private ILoggingService _log;
        private ISDR _driver = null;
        private IDemodulator _demodulator = null;

        private DateTime _lastStationTest = DateTime.MinValue;
        private Dictionary<long,bool> _stationOnFrequency = new Dictionary<long, bool>();

        public event DemodulatedEventHandler OnRawAudioDemodulated;

        public RTLSDRDriverConnector(ILoggingService loggingService, ISDR driver)
        {
            _log = loggingService;

            _log.Debug($"Initializing RTLSDR TCP-IP FM Driver Connector");

            //_UDPStreamer = new UDPStreamer(_log);
            _driverConfiguration = new DVBTDriverConfiguration();

            _driver = driver;
            _driver.OnDataReceived += _driver_OnDataReceived;

            _demodulator = new FMDemodulator(_log);
            _demodulator.OnDemodulated += _demodulator_OnDemodulated;
        }

        public static bool IsStationPresent(byte[] interleavedPcm16)
        {
            if (interleavedPcm16 == null || interleavedPcm16.Length < 4000)
                return false;

            int sampleCount = interleavedPcm16.Length / 4; // stereo 16-bit = 4 bytes/frame
            float prev = 0f;
            int zeroCrossings = 0;

            double sumRms = 0, sumRms2 = 0;
            double totalPower = 0;
            int window = 960; // ~10 ms @ 96 kHz
            int rmsSamples = 0;

            double[] rmsBuffer = new double[sampleCount / window + 1];
            int rmsIndex = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                short left = BitConverter.ToInt16(interleavedPcm16, i * 4);
                short right = BitConverter.ToInt16(interleavedPcm16, i * 4 + 2);
                float mono = (left + right) * 0.5f / short.MaxValue;

                // Zero crossing count
                if ((mono > 0 && prev <= 0) || (mono < 0 && prev >= 0))
                    zeroCrossings++;
                prev = mono;

                // Power accumulation
                double sq = mono * mono;
                sumRms += sq;
                rmsSamples++;
                totalPower += sq;

                if (rmsSamples >= window)
                {
                    double rms = Math.Sqrt(sumRms / rmsSamples);
                    rmsBuffer[rmsIndex++] = rms;
                    sumRms = 0;
                    rmsSamples = 0;
                }
            }

            // Compute variance of RMS values (dynamics)
            int n = rmsIndex;
            if (n < 2) return false;

            double mean = 0, var = 0;
            for (int i = 0; i < n; i++) mean += rmsBuffer[i];
            mean /= n;
            for (int i = 0; i < n; i++) var += (rmsBuffer[i] - mean) * (rmsBuffer[i] - mean);
            var /= n;

            // Average power of the signal
            double avgPower = totalPower / sampleCount;

            // Normalized zero-crossing rate
            double zcr = (double)zeroCrossings / sampleCount;

            // --- Heuristic thresholds (tune as needed) ---
            bool hasDynamics = var > 1e-5;     // real audio has changing RMS
            bool notTooNoisy = zcr < 0.15;     // noise crosses zero very often
            bool strongSignal = avgPower > 0.001; // reject weak stations or static

            return hasDynamics && notTooNoisy && strongSignal;
        }

        private void _demodulator_OnDemodulated(object? sender, EventArgs e)
        {
            if ((e is DataDemodulatedEventArgs de) &&
                OnRawAudioDemodulated != null)
            {
                if ((DateTime.Now - _lastStationTest).TotalMilliseconds>300)
                {
                    _lastStationTest = DateTime.Now;
                    var station = IsStationPresent(de.Data);
                    if (station && !_stationOnFrequency.ContainsKey(_driver.Frequency))
                    {
                        _stationOnFrequency.Add(_driver.Frequency, station);
                    }

                    _log.Debug($"Station: {station}");
                }

                OnRawAudioDemodulated(this, new DemodulatedEventArgs(de.Data)
                {
                     Description = new AudioDataDescription()
                     {
                          BitsPerSample = 16,
                          Channels = 2,
                          SampleRate = _demodulator.Samplerate
                     }
                });
            }
        }

        private void _driver_OnDataReceived(object? sender, OnDataReceivedEventArgs e)
        {
            if (_demodulator != null && e.Size>0)
            {
                _demodulator.AddSamples(e.Data, e.Size);

                // save raw data for analysis
                //RecordData(e.Data, e.Size);
            }
        }

        private Stream _recordStream = null;

        private void RecordData(byte[] data, int size)
        {
            var fileName = Path.Combine("/storage/emulated/0/Android/media/net.petrjanousek.DVBTTelevizor.MAUI/", $"{(_driver.Frequency / 1000)}_kHz.raw");

            if (!File.Exists(fileName))
            {
                _recordStream = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write);
            }

            _recordStream.Write(data, 0, size);
            _recordStream.Flush();
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
                return
                        _driverInstalled &&
                        _driver != null &&
                        _driver.State == DriverStateEnum.Connected;
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
                return $"udp://@localhost:{(_driver == null ? "1234" : "8012")}";
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

        public long Bitrate
        {
            get
            {
                if (!Connected)
                    return 0;

                return _driver.RTLBitrate;
            }
        }

        public long LastTunedFreq { get; set; } = 104000000;

        public event EventHandler StatusChanged;

        public Task CheckPIDs()
        {
            return Task.Run(() => { return; });
        }

        public Task<bool> CheckStatus()
        {
            return Task.Run(() =>
            {
                if (!_driverInstalled ||
                   _driver == null ||
                    _driver.State != DriverStateEnum.Connected)
                {
                    return false;
                }

                return _driver.State == DriverStateEnum.Connected;
            });
        }

        public void Connect()
        {
            _log.Info($"RTL SDR driver: Connecting");

            try
            {
                _driver.Settings.Streamport = _driverConfiguration.TransferPort;
                _driver.Settings.Port = _driverConfiguration.ControlPort;

                _driver.Init(new DriverInitializationResult());
                _driver.Installed = true;

                _driver.SetFrequency(Convert.ToInt32(LastTunedFreq)); // must be set before init due to Test driver

                _driver.SetSampleRate(_driver.Settings.SDRSampleRate);
                _driver.SetDirectSampling(0);
                _driver.SetFrequencyCorrection(0);
                _driver.SetGainMode(false);

                DriverInstalled = true;
                State = DVBTDriverStateEnum.Connected;

                _stationOnFrequency.Clear();
            }
            catch (Exception ex)
            {
                State = DVBTDriverStateEnum.Disconnected;
            }
        }

        public Task Disconnect()
        {
            return Task.Run(() =>
            {
                _driver.Disconnect();
                State = DVBTDriverStateEnum.Disconnected;
            });
        }

        public Task<bool> DriverSendingData(int readMsTimeout = 500)
        {
            return Task.Run(() => { return Connected && _driver.RTLBitrate > 0; });
        }

        public Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency = 88000000,
                    maxFrequency = 108000000,
                    frequencyStepSize = 1000
                };
            });
        }

        public Task<DVBTDriverStatus> GetStatus()
        {
            return Task.Run(() =>
            {
                var state = new DVBTDriverStatus()
                {
                    SuccessFlag = true
                };

                switch (_driver.State)
                {
                    case DriverStateEnum.DisConnected:
                    case DriverStateEnum.NotInitialized:
                    case DriverStateEnum.Error:
                        state.hasCarrier = 0;
                        state.hasSync = 0;
                        state.hasSignal = 0;
                        state.hasLock = 0;
                        state.rfStrengthPercentage = 0;
                    break;
                    case DriverStateEnum.Connected:
                        state.hasSignal = 1;
                        state.hasCarrier = 0;
                        state.hasSync = 0;
                        state.hasLock = 0;
                        state.rfStrengthPercentage = Convert.ToInt64(_demodulator?.PercentSignalPower);
                        break;
                }

                if (StatusChanged != null)
                {
                    StatusChanged(this, new DVBTDriverStatusChangedEventArgs() { Status = state });
                }

                return state;
            });
        }

        public Task<DVBTDriverVersion> GetVersion()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverVersion()
                {
                    SuccessFlag = true,
                    Version = 1
                };
            });
        }

        public Task<EITScanResult> ScanEPG(int msTimeout = 2000)
        {
            return Task.Run(() => { return new EITScanResult(); });
        }

        public Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return Task.Run(() =>
            {
                if (_stationOnFrequency.ContainsKey(_driver.Frequency) && _stationOnFrequency[_driver.Frequency])
                {
                    var dict = new Dictionary<ServiceDescriptor, long>();
                    dict.Add(new ServiceDescriptor()
                    {
                        Free = true,
                        Length = 0,
                        ProgramNumber = _driver.Frequency,
                        ProviderName = "FM radio",
                        ServiceName = $"{(_driver.Frequency / 1000000.0).ToString("N1")} FM ",
                        ServisType = (byte)DVBTDriverServiceType.Radio

                    }, _driver.Frequency);

                    return new DVBTDriverSearchProgramMapPIDsResult()
                    {
                        Result = DVBTDriverSearchProgramResultEnum.OK,
                        ServiceDescriptors = dict
                    };
                }
                else
                {
                    return new DVBTDriverSearchProgramMapPIDsResult()
                    {
                        Result = DVBTDriverSearchProgramResultEnum.NoProgramFound
                    };
                }
            });
        }


        public Task<DVBTDriverSearchPIDsResult> SearchProgramPIDs(long mapPID, bool setPIDsAndSync)
        {
            return Task.Run(() =>
            {
                return new DVBTDriverSearchPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK
                };
            });
        }

        public Task<DVBTDriverSearchAllPIDsResult> SearchProgramPIDs(List<long> MapPIDs)
        {
            return Task.Run(() =>
            {
                return new DVBTDriverSearchAllPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK
                };
            });
        }

        public Task<DVBTDriverResponse> SetPIDs(List<long> PIDs)
        {
            return Task.Run(() => {
                return new DVBTDriverResponse()
                {
                    SuccessFlag = true
                };
            });
        }

        public Task<DVBTDriverSearchPIDsResult> SetupChannelPIDs(long mapPID, bool fastTuning)
        {
            return Task.Run(() => {
                return new DVBTDriverSearchPIDsResult()
                {
                    PIDs = new List<long>(),
                    Result = DVBTDriverSearchProgramResultEnum.OK
                };
            });
        }

        public Task StartRecording(string path)
        {
            return Task.Run(() => { return; });
        }

        public void StartStream()
        {
        }

        public Task<bool> Stop()
        {
            return Task.Run(() => { return true; });
        }

        public void StopRecording()
        {
        }

        public void StopStream()
        {
        }

        public Task<DVBTDriverResponse> Tune(long frequency, long bandwidth, int deliverySystem)
        {
            _log.Info($"RTL SDR driver: Tuning {frequency}");

            return Task.Run(() =>
            {
                var successFlag = false;

                try
                {
                    if (_driver.State == DriverStateEnum.Connected)
                    {
                        _driver.SetFrequency(Convert.ToInt32(frequency));
                        successFlag = true;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }

                return new DVBTDriverResponse()
                {
                    SuccessFlag = successFlag
                };
            });
        }

        public async Task<DVBTDriverTuneResult> TuneEnhanced(long frequency, long bandWidth, int deliverySystem, bool fastTuning)
        {
            _log.Info($"RTLSDRTCPIPFMDriverConnector: TuneEnhanced freq {frequency / 1000} kHz");

            var status = await GetStatus();

            if (!status.SuccessFlag)
            {
                _log.Debug($"Getting status failed");
                return new DVBTDriverTuneResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.Error
                };
            }

            var tuneResult = await Tune(frequency, bandWidth, deliverySystem);
            if (!tuneResult.SuccessFlag)
            {
                _log.Debug($"Tune failed");
                return new DVBTDriverTuneResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.Error
                };
            }

            //_demodulator.ClearBuffer();

            //await Task.Delay(300);

            await Task.Delay(3500);

            //if (_stationOnFrequency.ContainsKey(_driver.Frequency) && _stationOnFrequency[_driver.Frequency])
            //{
                return new DVBTDriverTuneResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK,
                    SignalState = new DVBTDriverStatus()
                    {
                        hasCarrier = 1,
                        hasLock = 1,
                        hasSync = 1,
                        hasSignal = 1,
                        SuccessFlag = true,
                        rfStrengthPercentage = Convert.ToInt64(_demodulator.PercentSignalPower)
                    }
                };
            //}

            //return new DVBTDriverTuneResult()
            //{
            //    Result = DVBTDriverSearchProgramResultEnum.NoSignal,
            //    SignalState = new DVBTDriverStatus()
            //    {
            //        hasCarrier = 10,
            //        hasLock = 0,
            //        hasSync = 0,
            //        hasSignal = 0,
            //        SuccessFlag = true,
            //        rfStrengthPercentage = Convert.ToInt64(_demodulator.PercentSignalPower)
            //    }
            //};

        }

        public Task WaitForBufferPIDs(List<long> PIDs, int readMsTimeout = 500, int msTimeout = 6000)
        {
            return Task.Run(() => { return; });
        }

        public Task<DVBTDriverTuneResult> WaitForSignal(bool fastTuning)
        {
            return Task.Run( () => { return new DVBTDriverTuneResult();  } );
        }

        public int FrequencyMinKHz
        {
            get { return 88000; }
        }

        public int FrequencyMaxKHz
        {
            get { return 108000; }
        }

        public int BandwidthMinKHz
        {
            get { return 100; }
        }

        public int BandwidthMaxKHz
        {
            get { return 100; }
        }
    }
}
