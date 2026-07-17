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
    public class RTLSDRFMDriverConnector : RTLSDRDriverConnector
    {
        public RTLSDRFMDriverConnector(ILoggingService loggingService, ISDR driver, IDemodulator demodulator, int startupFrequency)
            : base(loggingService, driver, demodulator, startupFrequency)
        {
        }

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
            return Task.Run(() =>
            {
                if (LastFreqHasSignal)
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
    }
}
