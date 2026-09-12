using Android.OS.Storage;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.TV;
using LoggerService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class DVBTTelevizorConfiguration : CustomSharedPreferencesObject, ITVConfiguration
    {
        private ILoggingService _loggingService;
        private string _configDirectory = string.Empty;

        public bool Fullscreen
        {
            get
            {
                return GetPersistingSettingValue<bool>("Fullscreen");
            }
            set
            {
                SavePersistingSettingValue<bool>("Fullscreen", value);
            }
        }

        public AppFontSizeEnum AppFontSize
        {
            get
            {
                var index = GetPersistingSettingValue<int>("AppFontSize");
                return (AppFontSizeEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("AppFontSize", (int)value);
            }
        }

        public long DVBTBandwidthKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("DVBTBandwidthKHz", 8000);
            }
            set
            {
                SavePersistingSettingValue<long>("DVBTBandwidthKHz", value);
            }
        }

        public long FMDVBTBandwidthKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FMDVBTBandwidthKHz", 100);
            }
            set
            {
                SavePersistingSettingValue<long>("FMDVBTBandwidthKHz", value);
            }
        }

        public AppDriverTypeEnum AppDriverType
        {
            get
            {
                var index = GetPersistingSettingValue<int>("AppDriverType");
                return (AppDriverTypeEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("AppDriverType", (int)value);
            }
        }

        public bool PlayOnBackground
        {
            get
            {
                return GetPersistingSettingValue<bool>("PlayOnBackground");
            }
            set
            {
                SavePersistingSettingValue<bool>("PlayOnBackground", value);
            }
        }

        public bool ShowTVChannels
        {
            get
            {
                return !HideTVChannels;
            }
            set
            {
                HideTVChannels = !value;
            }
        }

        public bool HideTVChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("HideTVChannels", false);
            }
            set
            {
                SavePersistingSettingValue<bool>("HideTVChannels", value);
            }
        }

        public bool TuneDVBTEnabled
        {
            get
            {
                return GetPersistingSettingValue<bool>("TuneDVBTEnabled", true);
            }
            set
            {
                SavePersistingSettingValue<bool>("TuneDVBTEnabled", value);
            }
        }

        public bool TuneDVBT2Enabled
        {
            get
            {
                return GetPersistingSettingValue<bool>("TuneDVBT2Enabled", true);
            }
            set
            {
                SavePersistingSettingValue<bool>("TuneDVBT2Enabled", value);
            }
        }

        public bool TuneDVBTPreferred
        {
            get
            {
                return GetPersistingSettingValue<bool>("TuneDVBTPreferred", true);
            }
            set
            {
                SavePersistingSettingValue<bool>("TuneDVBTPreferred", value);
            }
        }

        public bool ShowNonFreeChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowNonFreeChannels", true);
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowNonFreeChannels", value);
            }
        }

        public bool ShowRadioChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowRadioChannels", true);
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowRadioChannels", value);
            }
        }

        public bool ShowOtherChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("ShowOtherChannels", false);
            }
            set
            {
                SavePersistingSettingValue<bool>("ShowOtherChannels", value);
            }
        }

        public bool EnableLogging
        {
            get
            {
                return GetPersistingSettingValue<bool>("EnableLogging");
            }
            set
            {
                SavePersistingSettingValue<bool>("EnableLogging", value);
            }
        }

        public string AutoPlayedChannelUniqueID
        {
            get
            {
                return GetPersistingSettingValue<string>("AutoPlayedChannelUniqueID");
            }
            set
            {
                SavePersistingSettingValue<string>("AutoPlayedChannelUniqueID", value);
            }
        }

        public DVBTTelevizorConfiguration(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider)
        {
            if (loggingProvider != null)
            {
                _loggingService = loggingProvider.GetLoggingService();
            } else
            {
                _loggingService = new BasicLoggingService();
            }

            if (publicDirectoryProvider != null)
            {
                _configDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
            }
        }

        private string ChannelsConfigFileName
        {
            get
            {
                return Path.Join(ConfigDirectory, "DVBTTelevizor.MAUI.channels.json");
            }
        }

        public string LoggingUDPIP
        {
            get
            {
                return GetPersistingSettingValue<string>("LoggingUDPIP", "10.0.0.2");
            }
            set
            {
                SavePersistingSettingValue<string>("LoggingUDPIP", value);
            }
        }

        public string ConfigDirectory
        {
            get
            {
                return _configDirectory;
            }
            set
            {
                _configDirectory = value;
            }
        }

        public int RemoteAccessServicePort
        {
            get
            {
                var port = GetPersistingSettingValue<int>("RemoteAccessServicePort");
                if (port == default(int))
                {
                    port = 49152;
                }

                return port;
            }
            set
            {
                SavePersistingSettingValue<int>("RemoteAccessServicePort", value);
            }
        }

        public string RemoteAccessServiceSecurityKey
        {
            get
            {
                var key = GetPersistingSettingValue<string>("RemoteAccessServiceSecurityKey");
                if (key == default(string))
                {
                    key = "DVBTTelevizor";
                }

                return key;
            }
            set { SavePersistingSettingValue<string>("RemoteAccessServiceSecurityKey", value); }
        }

        public string RemoteAccessServiceIP
        {
            get
            {
                var ip = GetPersistingSettingValue<string>("RemoteAccessServiceIP");
                if (ip == default(string))
                {
                    try
                    {
                        var ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                        ip = ipHostInfo.AddressList[0].ToString();
                    }
                    catch
                    {
                        ip = "192.168.1.10";
                    }
                }

                return ip;
            }
            set { SavePersistingSettingValue<string>("RemoteAccessServiceIP", value); }
        }

        public bool AllowRemoteAccessService
        {
            get
            {
                return GetPersistingSettingValue<bool>("AllowRemoteAccessService");
            }
            set
            {
                SavePersistingSettingValue<bool>("AllowRemoteAccessService", value);
            }
        }

        public long FrequencyFromKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FrequencyFromKHz", 474000);
            }
            set
            {
                SavePersistingSettingValue<long>("FrequencyFromKHz", value);
            }
        }

        public long FrequencyToKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FrequencyToKHz", 852000);
            }
            set
            {
                SavePersistingSettingValue<long>("FrequencyToKHz", value);
            }
        }

        public long FrequencyKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FrequencyKHz", 474000);
            }
            set
            {
                SavePersistingSettingValue<long>("FrequencyKHz", value);
            }
        }

        public long FMFrequencyFromKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FMFrequencyFromKHz", 88000);
            }
            set
            {
                SavePersistingSettingValue<long>("FMFrequencyFromKHz", value);
            }
        }

        public long FMFrequencyToKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FMFrequencyToKHz", 108000);
            }
            set
            {
                SavePersistingSettingValue<long>("FMFrequencyToKHz", value);
            }
        }
        public long FMFrequencyKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("FMFrequencyKHz", 104000);
            }
            set
            {
                SavePersistingSettingValue<long>("FMFrequencyKHz", value);
            }
        }

        public long DABFrequencyFromKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("DABFrequencyFromKHz", 174928);
            }
            set
            {
                SavePersistingSettingValue<long>("DABFrequencyFromKHz", value);
            }
        }

        public long DABFrequencyToKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("DABFrequencyToKHz", 239200);
            }
            set
            {
                SavePersistingSettingValue<long>("DABFrequencyToKHz", value);
            }
        }
        public long DABFrequencyKHz
        {
            get
            {
                return GetPersistingSettingValue<long>("DABFrequencyKHz", 174928);
            }
            set
            {
                SavePersistingSettingValue<long>("DABFrequencyKHz", value);
            }
        }

        public int SDRDriverPort
        {
            get
            {
                return GetPersistingSettingValue<int>("SDRDriverPort", 5658);
            }
            set
            {
                SavePersistingSettingValue<int>("SDRDriverPort", value);
            }
        }

        public int SDRDriverStreamPort
        {
            get
            {
                return GetPersistingSettingValue<int>("SDRDriverStreamPort", 5659);
            }
            set
            {
                SavePersistingSettingValue<int>("SDRDriverStreamPort", value);
            }
        }

        public int SDRSampleRate
        {
            get
            {
                return GetPersistingSettingValue<int>("SDRSampleRate", 1024000);
            }
            set
            {
                SavePersistingSettingValue<int>("SDRSampleRate", value);
            }
        }

        public ObservableCollection<Channel> GetChannels()
        {
            try
            {
                _loggingService.Debug("Loading channels");

                var json = GetPersistingSettingValue<string>("ChannelsJson");
                if (string.IsNullOrEmpty(json) && (File.Exists(ChannelsConfigFileName)))
                {
                    json = File.ReadAllText(ChannelsConfigFileName);
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var loadedChannels = JsonConvert.DeserializeObject<ObservableCollection<Channel>>(json);

                    if (loadedChannels != null && loadedChannels.Count > 0)
                    {
                        var sortedChannels = loadedChannels.OrderBy(p => p.Number.ToString().PadLeft(4,'0'));
                        return new ObservableCollection<Channel>(sortedChannels);
                    }
                }

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }

            return new ObservableCollection<Channel>();
        }

        public void SaveChannels(ObservableCollection<Channel> channels)
        {
            try
            {
                _loggingService.Info("Saving channels");

                var json = JsonConvert.SerializeObject(channels);

                SavePersistingSettingValue<string>("ChannelsJson", json);

                File.WriteAllText(ChannelsConfigFileName, json);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public string Language
        {
            get
            {
                return GetPersistingSettingValue<string>("Language");
            }
            set
            {
                SavePersistingSettingValue<string>("Language", value);
            }
        }

        public bool WriteToExternalDevice
        {
            get
            {
                return GetPersistingSettingValue<bool>("WriteToExternalDevice", false);
            }
            set
            {
                SavePersistingSettingValue<bool>("WriteToExternalDevice", value);
            }
        }

        public string ExternalDevicePath
        {
            get
            {
                return GetPersistingSettingValue<string>("ExternalDevicePath");
            }
            set
            {
                SavePersistingSettingValue<string>("ExternalDevicePath", value);
            }
        }

        public string ExternalDevicePathUri
        {
            get
            {
                return GetPersistingSettingValue<string>("ExternalDevicePathUri");
            }
            set
            {
                SavePersistingSettingValue<string>("ExternalDevicePathUri", value);
            }
        }

        public string OutputDirectory
        {
            get
            {
                return WriteToExternalDevice
                    ? GetExternalDeviceOutputDirectory()
                    : GetInternalDeviceOutputDirectory();
            }
        }

        private string GetExternalDeviceOutputDirectory()
        {
            if (!String.IsNullOrEmpty(ExternalDevicePath))
            {
                return ExternalDevicePath;
            }

            var externalPath = GetExternalPathFromStorageManager();
            if (!String.IsNullOrEmpty(externalPath))
            {
                return externalPath;
            }

            return GetExternalPathFromDirs();
        }

        private static string GetExternalPathFromStorageManager()
        {
            try
            {
                var context = Android.App.Application.Context;
                var storageManager = (Android.OS.Storage.StorageManager)context.GetSystemService(Android.Content.Context.StorageService);

                var volumeList = (Java.Lang.Object[])storageManager.Class.GetDeclaredMethod("getVolumeList").Invoke(storageManager);

                foreach (var storage in volumeList)
                {
                    if (storage is StorageVolume volume && !volume.IsPrimary && !volume.IsEmulated && volume.IsRemovable)
                    {
                        return volume.Directory.AbsolutePath;
                    }
                }
            }
            catch (Exception)
            {
                // fallback will be used
            }

            return string.Empty;
        }

        private static string GetExternalPathFromDirs()
        {
            try
            {
                var dirs = Android.App.Application.Context.GetExternalFilesDirs(null);
                foreach (var dir in dirs)
                {
                    if (dir != null && !dir.ToString().StartsWith("/storage/emulated/"))
                    {
                        return dir.ToString();
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return string.Empty;
        }

        private static string GetInternalDeviceOutputDirectory()
        {
            var path = GetExternalMediaDirectory();
            if (!String.IsNullOrEmpty(path))
            {
                return path;
            }

            path = GetSpecialFolderMyDocumentsDirectory();
            if (!String.IsNullOrEmpty(path))
            {
                return path;
            }

            try
            {
                var dir = Android.App.Application.Context.GetExternalFilesDir("");
                return dir != null ? dir.AbsolutePath : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string GetExternalMediaDirectory()
        {
            try
            {
                var pathToExternalMediaDirs = Android.App.Application.Context.GetExternalMediaDirs();
                if (pathToExternalMediaDirs != null && pathToExternalMediaDirs.Length > 0 && pathToExternalMediaDirs[0] != null)
                {
                    return pathToExternalMediaDirs[0].AbsolutePath;
                }
            }
            catch (Exception)
            {
                // fallback for older API
            }

            return string.Empty;
        }

        private static string GetSpecialFolderMyDocumentsDirectory()
        {
            try
            {
                var internalStorageDir = Android.App.Application.Context.GetExternalFilesDir(Environment.SpecialFolder.MyDocuments.ToString());
                if (internalStorageDir != null)
                {
                    return internalStorageDir.AbsolutePath;
                }
            }
            catch (Exception)
            {
                // fallback for older API
            }

            return string.Empty;
        }

        public string LastSelectedChannelUniqueIdentifier
        {
            get
            {
                return GetPersistingSettingValue<string>("LastSelectedChannelUniqueIdentifier");
            }
            set
            {
                SavePersistingSettingValue<string>("LastSelectedChannelUniqueIdentifier", value);
            }
        }

        public bool SledovaniTVEnabled
        {
            get
            {
                return GetPersistingSettingValue<bool>("SledovaniTVEnabled");
            }
            set
            {
                SavePersistingSettingValue<bool>("SledovaniTVEnabled", value);
            }
        }

        public bool SledovaniTVShowAdultChannels
        {
            get
            {
                return GetPersistingSettingValue<bool>("SledovaniTVShowAdultChannels");
            }
            set
            {
                SavePersistingSettingValue<bool>("SledovaniTVShowAdultChannels", value);
            }
        }

        public string SledovaniTVUserName
        {
            get
            {
                return GetPersistingSettingValue<string>("SledovaniTVUserName");
            }
            set
            {
                SavePersistingSettingValue<string>("SledovaniTVUserName", value);
            }
        }

        public string SledovaniTVPassword
        {
            get
            {
                return GetPersistingSettingValue<string>("SledovaniTVPassword");
            }
            set
            {
                SavePersistingSettingValue<string>("SledovaniTVPassword", value);
            }
        }

        public string SledovaniTVPIN
        {
            get
            {
                return GetPersistingSettingValue<string>("SledovaniTVPIN");
            }
            set
            {
                SavePersistingSettingValue<string>("SledovaniTVPIN", value);
            }
        }

        public string SledovaniTVDeviceID
        {
            get
            {
                return GetPersistingSettingValue<string>("SledovaniTVDeviceID");
            }
            set
            {
                SavePersistingSettingValue<string>("SledovaniTVDeviceID", value);
            }
        }

        public string SledovaniTVDevicePassword
        {
            get
            {
                return GetPersistingSettingValue<string>("SledovaniTVDevicePassword");
            }
            set
            {
                SavePersistingSettingValue<string>("SledovaniTVDevicePassword", value);
            }
        }

        public string FilteredMultiplexes
        {
            get
            {
                return GetPersistingSettingValue<string>("FilteredMultiplexes");
            }
            set
            {
                SavePersistingSettingValue<string>("FilteredMultiplexes", value);
            }
        }

        public bool UpdatedTo2026
        {
            get
            {
                return GetPersistingSettingValue<bool>("UpdatedTo2026");
            }
            set
            {
                SavePersistingSettingValue<bool>("UpdatedTo2026", value);
            }
        }

        public bool UpdatedTo2026_rev2
        {
            get
            {
                return GetPersistingSettingValue<bool>("UpdatedTo2026_rev2");
            }
            set
            {
                SavePersistingSettingValue<bool>("UpdatedTo2026_rev2", value);
            }
        }

        public bool RTLSDREnabled
        {
            get
            {
                return GetPersistingSettingValue<bool>("RTLSDREnabled");
            }
            set
            {
                SavePersistingSettingValue<bool>("RTLSDREnabled", value);
            }
        }

        public bool TestingMode
        {
            get
            {
                return GetPersistingSettingValue<bool>("TestingMode");
            }
            set
            {
                SavePersistingSettingValue<bool>("TestingMode", value);
            }
        }


        public GainEnum Gain
        {
            get
            {
                var index = GetPersistingSettingValue<int>("Gain");
                return (GainEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("Gain", (int)value);
            }
        }

        public int GainValue
        {
            get
            {
                return GetPersistingSettingValue<int>("GainValue", 0);
            }
            set
            {
                SavePersistingSettingValue<int>("GainValue", value);
            }
        }

        public bool AllowRemoteSDR
        {
            get
            {
                return GetPersistingSettingValue<bool>("AllowRemoteSDR");
            }
            set
            {
                SavePersistingSettingValue<bool>("AllowRemoteSDR", value);
            }
        }

        public string RemoteSDRIP
        {
            get
            {
                return GetPersistingSettingValue<string>("RemoteSDRIP");
            }
            set
            {
                SavePersistingSettingValue<string>("RemoteSDRIP", value);
            }
        }

        public int RemoteSDRPort
        {
            get
            {
                return GetPersistingSettingValue<int>("RemoteSDRPort", 1234);
            }
            set
            {
                SavePersistingSettingValue<int>("RemoteSDRPort", value);
            }
        }

        public bool AllowRemoteVLC
        {
            get
            {
                return GetPersistingSettingValue<bool>("AllowRemoteVLC");
            }
            set
            {
                SavePersistingSettingValue<bool>("AllowRemoteVLC", value);
            }
        }

        public string RemoteVLCIP
        {
            get
            {
                return GetPersistingSettingValue<string>("RemoteVLCIP");
            }
            set
            {
                SavePersistingSettingValue<string>("RemoteVLCIP", value);
            }
        }

        public int RemoteVLCPort
        {
            get
            {
                return GetPersistingSettingValue<int>("RemoteVLCPort", 8080);
            }
            set
            {
                SavePersistingSettingValue<int>("RemoteVLCPort", value);
            }
        }

        public string RemoteVLCPassword
        {
            get
            {
                return GetPersistingSettingValue<string>("RemoteVLCPassword");
            }
            set
            {
                SavePersistingSettingValue<string>("RemoteVLCPassword", value);
            }
        }
    }
}
