using LoggerService;
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

        public bool TuneDVBTPreferred { get; set; } = false;

        public long BandwidthKHz { get; set; } = 8000;

        public long FrequencyFromKHz { get; set; } = 474000;
        public long FrequencyToKHz { get; set; } = 852000;
        public long FrequencyKHz { get; set; } = 474000;

        public long DeviceFrequencyMinKHz { get; set; } = 474000;
        public long DeviceFrequencyMaxKHz { get; set; } = 852000;

        public static long DefaultFrequencyMinKHz { get; set; } = 474000;
        public static long DefaultFrequencyMaxKHz { get; set; } = 852000;

        public static long DefaultBandwidthKHz { get; set; } = 8000;

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
                 BandwidthKHz = BandwidthKHz,
                 FrequencyFromKHz = FrequencyFromKHz,
                 FrequencyToKHz = FrequencyToKHz,
                 FrequencyKHz = FrequencyKHz,
                 TuneDVBTPreferred = TuneDVBTPreferred,
                 TuningMode = TuningMode,
                 DeviceFrequencyMinKHz =  DeviceFrequencyMinKHz,
                 DeviceFrequencyMaxKHz = DeviceFrequencyMaxKHz
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

        public void LoadFromConfiguration(ITVConfiguration configuration)
        {
            DVBT = configuration.TuneDVBTEnabled;
            DVBT2 = configuration.TuneDVBT2Enabled;
            TuneDVBTPreferred = configuration.TuneDVBTPreferred;

            switch (configuration.DVBTDriverType)
            {
                case MAUI.DriverTypeEnum.RTLSDRDriver:
                    BandwidthKHz = configuration.FMDVBTBandwidthKHz;
                    FrequencyKHz = configuration.FMFrequencyKHz;
                    FrequencyFromKHz = configuration.FMFrequencyFromKHz;
                    FrequencyToKHz = configuration.FMFrequencyToKHz;

                    SetFMSettings();

                    break;

                case MAUI.DriverTypeEnum.AndroidDVBTDriver:
                default:
                    BandwidthKHz = configuration.DVBTBandwidthKHz;
                    FrequencyKHz = configuration.FrequencyKHz;
                    FrequencyFromKHz = configuration.FrequencyFromKHz;
                    FrequencyToKHz = configuration.FrequencyToKHz;

                    SetDVBTSettings();

                    break;
            }
        }

        public void SaveToConfiguration(ITVConfiguration configuration)
        {
            configuration.TuneDVBTEnabled = DVBT;
            configuration.TuneDVBT2Enabled = DVBT2;
            configuration.TuneDVBTPreferred = TuneDVBTPreferred;

            switch (configuration.DVBTDriverType)
            {
                case MAUI.DriverTypeEnum.RTLSDRDriver:
                    configuration.FMDVBTBandwidthKHz = BandwidthKHz;
                    configuration.FMFrequencyKHz = FrequencyKHz;
                    configuration.FMFrequencyFromKHz = FrequencyFromKHz;
                    configuration.FMFrequencyToKHz = FrequencyToKHz;
                    break;

                case MAUI.DriverTypeEnum.AndroidDVBTDriver:
                default:
                    configuration.DVBTBandwidthKHz = BandwidthKHz ;
                    configuration.FrequencyKHz = FrequencyKHz;
                    configuration.FrequencyFromKHz = FrequencyFromKHz;
                    configuration.FrequencyToKHz = FrequencyToKHz;
                    break;
            }
        }

        public void SetFMSettings()
        {
            FM = true;
            //_tuningSettings.FrequencyKHz = 88000;
            FrequencyFromKHz = 88000;
            FrequencyToKHz = 108000;
            FrequencyKHz = FrequencyFromKHz;

            DeviceFrequencyMinKHz = 88000;
            DeviceFrequencyMaxKHz = 108000;

            BandwidthKHz = 100;
        }

        public void SetDVBTSettings()
        {
            FM = false;

            FrequencyFromKHz = DefaultFrequencyMinKHz;
            FrequencyToKHz = DefaultFrequencyMaxKHz;
            FrequencyKHz = FrequencyFromKHz;

            DeviceFrequencyMinKHz = 174000; // 174.0 MHz - VHF high-band (band III) channel 7
            DeviceFrequencyMaxKHz = 858000; // 858.0 MHz - UHF band channel 69

            BandwidthKHz = DefaultBandwidthKHz;
        }

        public async Task SetFrequencies(IDriverConnector driver)
        {
            try
            {
                _loggingService.Info("SetFrequencies");

                if (BandwidthKHz < driver.BandwidthMinKHz ||
                    BandwidthKHz > driver.BandwidthMaxKHz)
                {
                    BandwidthKHz = driver.BandwidthMinKHz;
                }

                DeviceFrequencyMinKHz = driver.FrequencyMinKHz;
                DeviceFrequencyMaxKHz = driver.FrequencyMaxKHz;

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
                                DeviceFrequencyMinKHz = driver.FrequencyMinKHz;
                            }
                            if (!ValidFrequency(DeviceFrequencyMaxKHz, false))
                            {
                                DeviceFrequencyMaxKHz = driver.FrequencyMaxKHz;
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
    }
}
