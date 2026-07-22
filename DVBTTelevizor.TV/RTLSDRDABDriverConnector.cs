using LoggerService;
using MPEGTS;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.DAB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace DVBTTelevizor.TV
{
    public class RTLSDRDABDriverConnector : RTLSDRDriverConnector
    {
        private DateTime _lastStationTest = DateTime.MinValue;
        private Dictionary<long, bool> _stationOnFrequency = new Dictionary<long, bool>();
        private ILoggingService _loggingService;

        private string _aacFileName = null;

        public RTLSDRDABDriverConnector(ILoggingService loggingService, ISDR driver, IDemodulator demodulator, int startupFrequency)
            : base(loggingService, driver, demodulator, startupFrequency)
        {
            _loggingService = loggingService;
        }

        public override AppDriverTypeEnum DriverType => AppDriverTypeEnum.DAB;

        public override DriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DriverStreamTypeEnum.RAWAACAudio;
            }
        }

        public override void OnDataDemodulated(object? sender, EventArgs e)
        {
            base.OnDataDemodulated(sender, e);

            if (e is AACDataDemodulatedEventArgs aacargs &&
            aacargs.Data != null &&
            aacargs.Data.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(_aacFileName))
                {
                    var adtsHeaderLength = aacargs.ADTSHeader?.Length ?? 0;
                    var dataLength = aacargs.Data?.Length ?? 0;
                    var adtsFrame = new byte[adtsHeaderLength + dataLength];
                    if (aacargs.ADTSHeader != null)
                    {
                        Buffer.BlockCopy(aacargs.ADTSHeader, 0, adtsFrame, 0, adtsHeaderLength);
                    }
                    if (aacargs.Data != null)
                    {
                        Buffer.BlockCopy(aacargs.Data, 0, adtsFrame, adtsHeaderLength, dataLength);
                    }

                    File.AppendAllBytes(_aacFileName, adtsFrame);
                }
            }
        }

        public override string RecordFileName
        {
            get
            {
                return _aacFileName;
            }
        }

        public override void StartRecording(string path)
        {
            var fN = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + DVBTDriverConnector.GetHumanReadableFrequency(LastTunedFreq).ToString().Replace(" ", "_").Replace(".", "_") + "." + "aac";

            _aacFileName = Path.Combine(path, fN);

            base.StartRecording(path);
        }

        public override string StopRecording()
        {
            base.StopRecording();

            var fN = Path.GetFileName(_aacFileName);
            _aacFileName = null;
            return fN;
        }

        public override Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency = 174928000,
                    maxFrequency = 239200000,
                    frequencyStepSize = 1712000,
                    SuccessFlag = true
                };
            });
        }

        public override void Connect()
        {
            _stationOnFrequency.Clear();
            _driver.Settings.SDRSampleRate = AudioTools.DABSampleRate;
            base.Connect();
        }

        public override bool IsOnSpectrumSignal()
        {
            // check spectrum
            if (_spectrumWorker != null)
            {
                var spectrum = _spectrumWorker.GetScaledSpectrum(SpectrumWidth, SpectrumHeight);
                var isStationPresent = _spectrumWorker.IsDabStationPresent(spectrum);

                return isStationPresent;
            }

            return false;
        }

        public override Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return Task.Run(async () =>
            {
                await Task.Delay(10000); // wait for demodulator sync and find the services using service_found event

                return new DVBTDriverSearchProgramMapPIDsResult()
                {
                    Result = DVBTDriverSearchProgramResultEnum.OK,
                    ServiceDescriptors = new Dictionary<ServiceDescriptor, long>()
                };
            });
        }

        public override Task<DVBTDriverSearchPIDsResult> SetupChannelPIDs(long mapPID, bool fastTuning)
        {
            if (_demodulator is DABProcessor dab)
            {
                dab.SetProcessingService(Convert.ToInt32(mapPID));
            }
            return base.SetupChannelPIDs(mapPID, fastTuning);
        }
    }
}
