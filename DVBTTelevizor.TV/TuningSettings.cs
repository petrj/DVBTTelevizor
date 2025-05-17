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

        public long DeviceFrequencyMinKHz { get; set; } = 474000;
        public long DeviceFrequencyMaxKHz { get; set; } = 852000;

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

                // TODO: load/save bandwidth/freq (min/max?)
                BandwidthKHz = driver.BandwidthMinKHz;

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
