using Android.OS.Storage;
using CommunityToolkit.Mvvm.Messaging;
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

        public DVBTDriverTypeEnum DVBTDriverType
        {
            get
            {
                var index = GetPersistingSettingValue<int>("DVBTDriverType");
                return (DVBTDriverTypeEnum)index;
            }
            set
            {
                SavePersistingSettingValue<int>("DVBTDriverType", (int)value);
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
                return GetPersistingSettingValue<bool>("HideTVChannels", true);
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
                return GetPersistingSettingValue<bool>("ShowRadioChannels");
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
                return GetPersistingSettingValue<bool>("ShowOtherChannels");
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

        public string AutoPlayedChannelFrequencyAndMapPID
        {
            get
            {
                return GetPersistingSettingValue<string>("ChannelAutoPlayedAfterStart");
            }
            set
            {
                SavePersistingSettingValue<string>("ChannelAutoPlayedAfterStart", value);
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
                        return loadedChannels;
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
                // Android output (record) directory
                if (WriteToExternalDevice)
                {
                    // external device

                    if (!String.IsNullOrEmpty(ExternalDevicePath))
                    {
                        return ExternalDevicePath;
                    }

                    var externalPath = "";

                    try
                    {
                        var context = Android.App.Application.Context;
                        var storageManager = (Android.OS.Storage.StorageManager)context.GetSystemService(Android.Content.Context.StorageService);

                        var volumeList = (Java.Lang.Object[])storageManager.Class.GetDeclaredMethod("getVolumeList").Invoke(storageManager);

                        var list = new List<string>();

                        foreach (var storage in volumeList)
                        {
                            if (storage is StorageVolume volume)
                            {
                                if (volume.IsPrimary || volume.IsEmulated || !volume.IsRemovable)
                                {
                                    continue;
                                }

                                // first external device
                                externalPath = volume.Directory.AbsolutePath;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // fallback for older API:

                        var dirs = Android.App.Application.Context.GetExternalFilesDirs(null);
                        foreach (var dir in dirs)
                        {
                            if (dir.ToString().StartsWith("/storage/emulated/"))
                            {
                                continue;
                            }

                            // first external device
                            externalPath = dir.ToString();
                            break;
                        }
                    }

                    //if (externalPath.EndsWith("files"))
                    //{
                    //    externalPath = externalPath.Substring(0, externalPath.Length - 5); // remove "files" from the end
                    //}

                    return externalPath;

                } else
                {
                    // internal device

                    try
                    {
                        // internal storage - always writable directory
                        try
                        {
                            var pathToExternalMediaDirs = Android.App.Application.Context.GetExternalMediaDirs();

                            if (pathToExternalMediaDirs.Length == 0)
                                throw new DirectoryNotFoundException();

                            return pathToExternalMediaDirs[0].AbsolutePath;
                        }
                        catch
                        {
                            // fallback for older API:

                            var internalStorageDir = Android.App.Application.Context.GetExternalFilesDir(Environment.SpecialFolder.MyDocuments.ToString());

                            return internalStorageDir.AbsolutePath;
                        }
                    }
                    catch
                    {
                        var dir = Android.App.Application.Context.GetExternalFilesDir("");

                        return dir.AbsolutePath;
                    }
                }
            }
        }
    }
}
