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

        public static long DefaultBandwidthKHz { get; set; } = 8000;
        public static long DefaultFrequencyKHz { get; set; } = 474000;
        public static long FrequencyMinKHz { get; set; } = 174000; // 174.0 MHz - VHF high-band (band III) channel 7
        public static long FrequencyMaxKHz { get; set; } = 858000; // 858.0 MHz - UHF band channel 69

        public bool ValidFrequency(long freq, bool device)
        {
            var dvbtValid = ((freq >= TuningSettings.FrequencyMinKHz) && (freq <= TuningSettings.FrequencyMaxKHz));
            var deviceValid = ((freq >= DeviceFrequencyFromKHz) && (freq <= DeviceFrequencyToKHz));

            return device ? deviceValid && dvbtValid : dvbtValid;
        }

        public async Task SetFrequencies(ITVConfiguration configuration, IDriverConnector driver, ILoggingService loggingService)
        {
            try
            {
                loggingService.Info("SetFrequencies");

                // bandwidth
                BandwidthKHz = configuration.DVBTBandwidthKHz;
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
                            if (!ValidFrequency(TuningSettings.DefaultFrequencyKHz, false))
                            {
                                TuningSettings.DefaultFrequencyKHz = TuningSettings.FrequencyMinKHz;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        loggingService.Error(ex);
                    }
                }

                FrequencyKHz = configuration.FrequencyKHz;
                if (!ValidFrequency(FrequencyKHz, true))
                {
                    FrequencyKHz = TuningSettings.DefaultFrequencyKHz;
                }

                FrequencyFromKHz = configuration.FrequencyFromKHz;
                if (!ValidFrequency(FrequencyFromKHz, true))
                {
                    FrequencyFromKHz = DeviceFrequencyFromKHz;
                }

                FrequencyToKHz = configuration.FrequencyToKHz;
                if (!ValidFrequency(FrequencyToKHz, true))
                {
                    FrequencyToKHz = DeviceFrequencyToKHz;
                }
            }
            catch (Exception ex)
            {
                loggingService.Error(ex);
            }
            finally
            {

            }
        }
    }
}
