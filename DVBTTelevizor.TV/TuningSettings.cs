using DVBTTelevizor.TV;
using LoggerService;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public class TuningSettings
    {
        ILoggingService _loggingService = null;

        public TuneModeEnum TuningMode { get; set; } = TuneModeEnum.Automatic;

        public bool DVBT { get; set; } = true;
        public bool DVBT2 { get; set; } = true;
        public bool FM { get; set; } = false;
        public bool DAB { get; set; } = false;

        public bool TuneDVBTPreferred { get; set; } = false;

        public long BandwidthKHz { get; set; } = 8000;

        public long FrequencyFromKHz { get; set; } = 474000;
        public long FrequencyToKHz { get; set; } = 852000;
        public long FrequencyKHz { get; set; } = 474000;

        public long DeviceFrequencyMinKHz { get; set; } = 474000;
        public long DeviceFrequencyMaxKHz { get; set; } = 852000;

        public long DeviceBandWidthMinKHz { get; set; } = 1700;
        public long DeviceBandWidthMaxKHz { get; set; } = 10000;


        public long DefaultFrequencyMinKHz { get; set; } = 474000;
        public long DefaultFrequencyMaxKHz { get; set; } = 852000;

        public long DefaultBandwidthKHz { get; set; } = 8000;
        public long DefaultDABBandwidthKHz { get; set; } = 1712;

        public TuningSettings(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        public TuningSettings Clone(ILoggingService loggingService)
        {
            return new TuningSettings(loggingService)
            {
                 DVBT = DVBT,
                 DVBT2 = DVBT2,
                 FM = FM,
                 DAB = DAB,
                 BandwidthKHz = BandwidthKHz,
                 FrequencyFromKHz = FrequencyFromKHz,
                 FrequencyToKHz = FrequencyToKHz,
                 FrequencyKHz = FrequencyKHz,
                 TuneDVBTPreferred = TuneDVBTPreferred,
                 TuningMode = TuningMode,
                 DeviceFrequencyMinKHz =  DeviceFrequencyMinKHz,
                 DeviceFrequencyMaxKHz = DeviceFrequencyMaxKHz,
                 DefaultBandwidthKHz = DefaultBandwidthKHz,
                 DefaultFrequencyMinKHz = DefaultFrequencyMinKHz,
                 DefaultFrequencyMaxKHz = DefaultFrequencyMaxKHz,
                 DeviceBandWidthMinKHz = DeviceBandWidthMinKHz,
                 DeviceBandWidthMaxKHz = DeviceBandWidthMaxKHz
            };
        }

        public bool ValidFrequency(long freq, bool device)
        {
            /*
            var dvbtValid = ((freq >= TuningSettings.FrequencyMinKHz) && (freq <= TuningSettings.FrequencyMaxKHz));
            var deviceValid = ((freq >= DeviceFrequencyFromKHz) && (freq <= DeviceFrequencyToKHz));

            return device ? deviceValid && dvbtValid : dvbtValid;
            */

            return (freq >= DeviceFrequencyMinKHz) && (freq <= DeviceFrequencyMaxKHz);
        }

        public void LoadFromConfiguration(ITVConfiguration configuration, AppDriverTypeEnum driverType)
        {

            switch (driverType)
            {
                case AppDriverTypeEnum.FM:

                    SetFMSettings();

                    //BandwidthKHz = configuration.FMDVBTBandwidthKHz;
                    FrequencyKHz = configuration.FMFrequencyKHz;
                    FrequencyFromKHz = configuration.FMFrequencyFromKHz;
                    FrequencyToKHz = configuration.FMFrequencyToKHz;

                    break;

                case AppDriverTypeEnum.DAB:

                    SetDABSettings();

                    FrequencyKHz = configuration.DABFrequencyKHz;
                    FrequencyFromKHz = configuration.DABFrequencyFromKHz;
                    FrequencyToKHz = configuration.DABFrequencyToKHz;

                    break;

                case AppDriverTypeEnum.DVBT:
                default:
                    SetDVBTSettings();

                    BandwidthKHz = configuration.DVBTBandwidthKHz;
                    FrequencyKHz = configuration.FrequencyKHz;
                    FrequencyFromKHz = configuration.FrequencyFromKHz;
                    FrequencyToKHz = configuration.FrequencyToKHz;
                    DVBT = configuration.TuneDVBTEnabled;
                    DVBT2 = configuration.TuneDVBT2Enabled;
                    TuneDVBTPreferred = configuration.TuneDVBTPreferred;

                    break;
            }
        }

        public void SetFMSettings()
        {
            FM = true;
            DAB = false;
            DVBT = false;
            DVBT2 = false;
            //_tuningSettings.FrequencyKHz = 88000;
            FrequencyFromKHz = 88000;
            FrequencyToKHz = 108000;
            FrequencyKHz = FrequencyFromKHz;

            DeviceFrequencyMinKHz = 88000;
            DeviceFrequencyMaxKHz = 108000;

            DeviceBandWidthMinKHz = 100;
            DeviceBandWidthMaxKHz = 100;
            DefaultBandwidthKHz = 100;

            DefaultFrequencyMinKHz = DeviceFrequencyMinKHz;
            DefaultFrequencyMaxKHz = DeviceFrequencyMaxKHz;

            BandwidthKHz = DefaultBandwidthKHz;
        }

        public void SetDABSettings()
        {
            DAB = true;
            FM = false;
            DVBT = false;
            DVBT2 = false;

            DeviceFrequencyMinKHz = 174928;
            DeviceFrequencyMaxKHz = 239200;

            FrequencyFromKHz = 174928; // 5A
            FrequencyToKHz = 239200;  // 13F
            FrequencyKHz = FrequencyFromKHz;

            // DAB has dynamic bandwidths, but we set some defaults
            DeviceBandWidthMinKHz = 1712;
            DeviceBandWidthMaxKHz = 1712;
            DefaultBandwidthKHz = 1712;

            DefaultFrequencyMinKHz = DeviceFrequencyMinKHz;
            DefaultFrequencyMaxKHz = DeviceFrequencyMaxKHz;

            BandwidthKHz = DefaultDABBandwidthKHz;
        }

        public void SetDVBTSettings()
        {
            FM = false;
            DAB = false;
            DVBT = true;
            DVBT2 = true;

            FrequencyFromKHz = DefaultFrequencyMinKHz;
            FrequencyToKHz = DefaultFrequencyMaxKHz;
            FrequencyKHz = FrequencyFromKHz;

            DeviceFrequencyMinKHz = 174000; // 174.0 MHz - VHF high-band (band III) channel 7
            DeviceFrequencyMaxKHz = 858000; // 858.0 MHz - UHF band channel 69

            DeviceBandWidthMinKHz = 1700;
            DeviceBandWidthMaxKHz = 10000;

            DefaultBandwidthKHz = 8000;

            DefaultFrequencyMinKHz = 474000;
            DefaultFrequencyMaxKHz = DeviceFrequencyMaxKHz;

            BandwidthKHz = DefaultBandwidthKHz;
        }


        public async Task SetFrequencies(IDriverConnector driver)
        {
            try
            {
                _loggingService.Info("SetFrequencies");

                if (BandwidthKHz < DeviceBandWidthMinKHz ||
                    BandwidthKHz > DeviceBandWidthMaxKHz)
                {
                    BandwidthKHz = DefaultBandwidthKHz;
                }

                //DeviceFrequencyMinKHz = driver.FrequencyMinKHz;
                //DeviceFrequencyMaxKHz = driver.FrequencyMaxKHz;

                if (driver.Connected)
                {
                    try
                    {
                        var cap = await driver.GetCapabalities();

                        // setting min/max frequencies from device
                        if (cap.SuccessFlag)
                        {
                            DeviceFrequencyMinKHz = cap.minFrequency / 1000;
                            DeviceFrequencyMaxKHz = cap.maxFrequency / 1000;

                            if (!ValidFrequency(DeviceFrequencyMinKHz, false))
                            {
                                DeviceFrequencyMinKHz = DeviceFrequencyMinKHz;
                            }
                            if (!ValidFrequency(DeviceFrequencyMaxKHz, false))
                            {
                                DeviceFrequencyMaxKHz = DeviceFrequencyMaxKHz;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.Error(ex);
                    }
                }

                // fix

                if (!ValidFrequency(FrequencyKHz, true))
                {
                    FrequencyKHz = DeviceFrequencyMinKHz;
                }

                if (!ValidFrequency(FrequencyFromKHz, true))
                {
                    FrequencyFromKHz = DeviceFrequencyMinKHz;
                }

                if (!ValidFrequency(FrequencyToKHz, true))
                {
                    FrequencyToKHz = DeviceFrequencyMaxKHz;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
            finally
            {

            }
        }

        public int RoundFrequencyKHz(double freq, double min, double max)
        {
            var freqLong = Convert.ToInt64(freq);

            if (FM)
            {
                // round to bandwith

                var startFreq = AudioTools.FMMinFreq - min;

                var stepFreq = Math.Round(Math.Truncate(Convert.ToDecimal(freq - startFreq) / Convert.ToDecimal(BandwidthKHz)));

                var freqRounded = Convert.ToInt64(Convert.ToDecimal(startFreq) + stepFreq * BandwidthKHz);

                return Convert.ToInt32(freqRounded);
            }

            if (DAB)
            {
                // there is not constant bandwidth, so rounding is different
                var minFreqDist = long.MaxValue;
                long freqRounded = AudioTools.DABMinFreq;
                foreach (var f in AudioTools.DabFrequenciesHz)
                {
                    var dist = f.Value - freqLong * 1000;
                    if (Math.Abs(dist) < Math.Abs(minFreqDist))
                    {
                        minFreqDist = dist;
                        freqRounded = f.Value / 1000;
                    }
                }
                return Convert.ToInt32(freqRounded);
            }

            //if (!ValidFrequency(freqLong, true))
            //    return Convert.ToInt32(freqLong);

            return (int)Math.Round(freq);
        }

        public double RoundFrequencyKHzParts(double freq, out string wholePart, out string decimalPart)
        {
            var freqLong = Convert.ToInt64(freq);
            wholePart = "";
            decimalPart = "";

            if (FM)
            {
                // round to bandwith

                var startFreq = AudioTools.FMMinFreq - AudioTools.FMMinFreq/1000;

                var stepFreq = Math.Round(Math.Truncate(Convert.ToDecimal(freq - startFreq) / Convert.ToDecimal(BandwidthKHz)));

                var freqRounded = Convert.ToInt64(Convert.ToDecimal(startFreq) + stepFreq * BandwidthKHz);

                wholePart = (freqRounded / 1000).ToString();

                decimalPart = (freqRounded % 1000).ToString().PadLeft(3, '0');

                return Convert.ToDouble(freqRounded);
            }

            if (DAB)
            {
                // there is not constant bandwidth, so rounding is different
                var minFreqDist = long.MaxValue;
                long freqRounded = AudioTools.DABMinFreq;
                foreach (var f in AudioTools.DabFrequenciesHz)
                {
                    var dist = f.Value - freqLong * 1000;
                    if (Math.Abs(dist) < Math.Abs(minFreqDist))
                    {
                        minFreqDist = dist;
                        freqRounded = f.Value / 1000;
                    }
                }

                wholePart = (freqRounded / 1000).ToString();
                decimalPart = "." + (freqRounded % 1000).ToString().PadLeft(3, '0');

                if (AudioTools.FrequenciesDabMHz.ContainsKey(freqRounded/1000.0))
                {
                    wholePart = AudioTools.FrequenciesDabMHz[freqRounded / 1000.0];
                    decimalPart = "";
                }

                return Convert.ToDouble(freqRounded);
            }

            return Convert.ToDouble(freq);
        }
    }
}
