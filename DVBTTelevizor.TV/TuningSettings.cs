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

        public bool TuneDVBTPreferred { get; set; } = false;

        public long BandwidthKHz { get; set; } = 8000;

        public long FrequencyFromKHz { get; set; } = 474000;
        public long FrequencyToKHz { get; set; } = 852000;

        public long FrequencyKHz { get; set; } = 474000;

        public long DeviceFrequencyFromKHz { get; set; } = 474000;
        public long DeviceFrequencyToKHz { get; set; } = 852000;

        public long DefaultFrequencyKHz { get; set; } = 474000;
        public long DefaultFrequencyFromKHz { get; set; } = 474000;
        public long DefaultFrequencyToKHz { get; set; } = 852000;

        public const long DefaultBandwidthKHz = 8000;
        public const long FrequencyMinKHz = 174000; // 174.0 MHz - VHF high-band (band III) channel 7
        public const long FrequencyMaxKHz = 858000; // 858.0 MHz - UHF band channel 69

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
                 BandwidthKHz = BandwidthKHz,
                 FrequencyFromKHz = FrequencyFromKHz,
                 FrequencyToKHz = FrequencyToKHz,
                 FrequencyKHz = FrequencyKHz,
                 TuneDVBTPreferred = TuneDVBTPreferred,
                 TuningMode = TuningMode,
                 DefaultFrequencyFromKHz = DefaultFrequencyFromKHz,
                 DefaultFrequencyToKHz = DefaultFrequencyToKHz,
                 DefaultFrequencyKHz = DefaultFrequencyKHz,
                 DeviceFrequencyFromKHz =  DeviceFrequencyFromKHz,
                 DeviceFrequencyToKHz = DeviceFrequencyToKHz
            };
        }

        public bool ValidFrequency(long freq, bool device)
        {
            var dvbtValid = ((freq >= TuningSettings.FrequencyMinKHz) && (freq <= TuningSettings.FrequencyMaxKHz));
            var deviceValid = ((freq >= DeviceFrequencyFromKHz) && (freq <= DeviceFrequencyToKHz));

            return device ? deviceValid && dvbtValid : dvbtValid;
        }

        public void LoadFromConfiguration(ITVConfiguration configuration)
        {
            BandwidthKHz = configuration.DVBTBandwidthKHz;

            FrequencyKHz = configuration.FrequencyKHz;
            FrequencyFromKHz = configuration.FrequencyFromKHz;
            FrequencyToKHz = configuration.FrequencyToKHz;
        }


        public void SaveToConfiguration(ITVConfiguration configuration)
        {
            configuration.FrequencyKHz = FrequencyKHz;
            configuration.FrequencyFromKHz = FrequencyFromKHz;
            configuration.FrequencyToKHz = FrequencyToKHz;
            configuration.DVBTBandwidthKHz = BandwidthKHz;
        }

        public async Task SetFrequencies(IDriverConnector driver)
        {
            try
            {
                _loggingService.Info("SetFrequencies");

                // bandwidth
                if (BandwidthKHz == default)
                {
                    BandwidthKHz = TuningSettings.DefaultBandwidthKHz;
                }

                DeviceFrequencyFromKHz = TuningSettings.FrequencyMinKHz;
                DeviceFrequencyToKHz = TuningSettings.FrequencyMaxKHz;

                if (driver.Connected)
                {
                    try
                    {
                        var cap = await driver.GetCapabalities();

                        // setting min/max frequencies from device
                        if (cap.SuccessFlag)
                        {
                            DeviceFrequencyFromKHz = cap.minFrequency / 1000;
                            DeviceFrequencyToKHz = cap.maxFrequency / 1000;

                            if (!ValidFrequency(DeviceFrequencyFromKHz, false))
                            {
                                DeviceFrequencyFromKHz = TuningSettings.FrequencyMinKHz;
                            }
                            if (!ValidFrequency(DeviceFrequencyToKHz, false))
                            {
                                DeviceFrequencyToKHz = TuningSettings.FrequencyMaxKHz;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        _loggingService.Error(ex);
                    }
                }

                // fix default frequencies

                if (!ValidFrequency(DefaultFrequencyKHz, true))
                {
                    DefaultFrequencyKHz = DeviceFrequencyFromKHz;
                }

                if (!ValidFrequency(DefaultFrequencyFromKHz, true))
                {
                    DefaultFrequencyFromKHz = DeviceFrequencyFromKHz;
                }

                if (!ValidFrequency(DefaultFrequencyToKHz, true))
                {
                    DefaultFrequencyToKHz = DeviceFrequencyToKHz;
                }

                // fix

                if (!ValidFrequency(FrequencyKHz, true))
                {
                    FrequencyKHz = DefaultFrequencyKHz;
                }

                if (!ValidFrequency(FrequencyFromKHz, true))
                {
                    FrequencyFromKHz = DefaultFrequencyFromKHz;
                }

                if (!ValidFrequency(FrequencyToKHz, true))
                {
                    FrequencyToKHz = DefaultFrequencyToKHz;
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
