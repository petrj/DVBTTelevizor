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
    }
}
