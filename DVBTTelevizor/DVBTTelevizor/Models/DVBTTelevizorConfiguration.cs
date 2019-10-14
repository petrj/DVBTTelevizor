using Android.Content;
using Android.Preferences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class DVBTTelevizorConfiguration : CustomSharedPreferencesObject
    {
        public string DriverConfigurationJSON
        {
            get
            {
                return GetPersistingSettingValue<string>("DriverConfiguration");
            }
            set
            {
                SavePersistingSettingValue<string>("DriverConfiguration", value);
            }
        }

        public DVBTDriverConfiguration Driver
        {
            get
            {
                var val = DriverConfigurationJSON;
                if (string.IsNullOrEmpty(val))
                    return null;

                var driverConfiguration = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(val);                

                return driverConfiguration;
            }
            set
            {
                if (value == null)
                {
                    DriverConfigurationJSON = string.Empty;
                } else
                {
                    DriverConfigurationJSON = value.ToString();
                }                
            }
        }
    }
}
