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
    public class RTLSDRFMDriverConnector : RTLSDRDriverConnector
    {
        public RTLSDRFMDriverConnector(ILoggingService loggingService, ISDR driver, IDemodulator demodulator, int startupFrequency)
            : base(loggingService, driver, demodulator, startupFrequency)
        {
        }

        private Wave? _wave = null;
        private string _waveFileName = null;

        public override AppDriverTypeEnum DriverType => AppDriverTypeEnum.FM;

        public override Task<DVBTDriverCapabilities> GetCapabalities()
        {
            return Task.Run(() =>
            {
                return new DVBTDriverCapabilities()
                {
                    supportedDeliverySystems = 0,
                    minFrequency = 88000000,
                    maxFrequency = 108000000,
                    frequencyStepSize = 1000,
                    SuccessFlag = true
                };
            });
        }

        public override void OnDataDemodulated(object? sender, EventArgs e)
        {
            base.OnDataDemodulated(sender, e);

            if (e is DataDemodulatedEventArgs dargs &&
            dargs.Data != null &&
            dargs.Data.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(_waveFileName))
                {
                    if ((_wave == null) && (dargs.AudioDescription != null))
                    {
                        _wave = new Wave();
                        _wave.CreateWaveFile(_waveFileName, dargs.AudioDescription);
                    }
                    if (_wave != null && dargs.Data != null)
                    {
                        _wave.WriteSampleData(dargs.Data);
                    }
                }
            }
        }

        public override string RecordFileName
        {
            get
            {
                return _waveFileName;
            }
        }

        public override void StartRecording(string path)
        {
            var fN = DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + DVBTDriverConnector.GetHumanReadableFrequency(LastTunedFreq).ToString().Replace(" ", "_").Replace(".", "_")+"." + "wav";

            _waveFileName = Path.Combine(path, fN);
            _wave = null;

            base.StartRecording(path);
        }

        public override string StopRecording()
        {
            base.StopRecording();

            if (_wave != null)
            {
                _wave.CloseWaveFile();
                _wave = null;
            }
            var fN = Path.GetFileName(_waveFileName);
            _waveFileName = null;
            return fN;
        }

        public override DriverStreamTypeEnum DVBTDriverStreamType
        {
            get
            {
                return DriverStreamTypeEnum.RAWPCMAudio;
            }
        }

        public override void Connect()
        {
            _driver.Settings.SDRSampleRate = AudioTools.FMSampleRate;
            base.Connect();
        }

        public override bool IsOnSpectrumSignal()
        {
            // check spectrum
            if (_spectrumWorker != null)
            {
                var spectrum = _spectrumWorker.GetScaledSpectrum(SpectrumWidth, SpectrumHeight);

                var medianNoise = SpectrumWorker.GetMedian(spectrum);
                var fmPeaks = SpectrumWorker.GetPeaksAroundCenter(spectrum, medianNoise, thresholdOffset: SpectrumHThresholdOffset);

                if (fmPeaks.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public override Task<DVBTDriverSearchProgramMapPIDsResult> SearchProgramMapPIDs(bool tunePID0and17 = true)
        {
            return Task.Run(async () =>
            {
                if (LastFreqHasSignal)
                {
                    Demodulator_OnServiceFound(this, new FMServiceFoundEventArgs()
                    {
                        Percents = 100
                    });

                    await Task.Delay(3000);   // play tuned radio effect for 1 second

                    return new DVBTDriverSearchProgramMapPIDsResult()
                    {
                        Result = DVBTDriverSearchProgramResultEnum.OK,
                        ServiceDescriptors = new Dictionary<ServiceDescriptor, long>()
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
    }
}
