using Android.Content;
using Android.Preferences;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class DVBTTelevizorConfiguration : CustomSharedPreferencesObject
    {
        public string StorageFolder
        {
            get
            {
                var val = GetPersistingSettingValue<string>("StorageFolder");
                if (string.IsNullOrEmpty(val))
                {
                    val = "/storage/emulated/0/Download/";
                }
                return val;
            }
            set
            {
                SavePersistingSettingValue<string>("StorageFolder", value);
            }
        }

        public ObservableCollection<DVBTChannel> Channels
        {
            get
            {
                var val = GetPersistingSettingValue<string>("ChannelsJson");
                if (!string.IsNullOrEmpty(val))
                {
                    return JsonConvert.DeserializeObject<ObservableCollection<DVBTChannel>>(val);
                }
                return null;
            }
            set
            {
                SavePersistingSettingValue<string>("ChannelsJson", JsonConvert.SerializeObject(value));
            }
        }

        public bool AutoInitAfterStart
        {
            get
            {
               return GetPersistingSettingValue<bool>("AutoInitAfterStart");
            }
            set
            {
                SavePersistingSettingValue<bool>("AutoInitAfterStart", value);
            }
        }

        public bool ShowRadioChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowRadioChannels");
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowRadioChannels", value);
            }
        }
    }
}
