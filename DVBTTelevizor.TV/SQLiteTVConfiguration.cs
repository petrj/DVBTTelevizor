using DVBTTelevizor.MAUI;
using LoggerService;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor
{
    public class SQLiteTVConfiguration : ITVConfiguration
    {
        private ILoggingService _loggingService;
        private string _configDBPath = string.Empty;
        private string _configDirectory = string.Empty;

        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        public SQLiteTVConfiguration(ILoggingProvider loggingProvider, IPublicDirectoryProvider publicDirectoryProvider)
        {
            _loggingService = loggingProvider.GetLoggingService();
            _configDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
            _configDBPath = Path.Join(_configDirectory, "DVBTTelevizor.MAUI.config.sqlite");

            InitDB();
        }

        private string ConnectionString
        {
            get
            {
                return $"Data Source={_configDBPath};";
            }
        }

        private void InitDB()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();

                string createTableQuery = "CREATE TABLE IF NOT EXISTS Configuration (Key TEXT PRIMARY KEY, Value TEXT);";
                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Vložení nebo aktualizace hodnoty v konfiguraci
                //SetConfigValue(connection, "AppName", "MojeAplikace");
                //SetConfigValue(connection, "Version", "1.0.0");

                // Čtení hodnoty z konfigurace
                //string appName = GetConfigValue(connection, "AppName");
                //Console.WriteLine($"AppName: {appName}");
            }
        }

        protected string GetPersistingSettingStringValue(string key, string defaultValue = "")
        {
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();

                    string selectQuery = "SELECT Value FROM Configuration WHERE Key = @key;";
                    using (var command = new SqliteCommand(selectQuery, connection))
                    {
                        command.Parameters.AddWithValue("@key", key);
                        object result = command.ExecuteScalar();
                        return result?.ToString() ?? "";
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }

            return String.Empty;
        }

        protected T GetPersistingSettingValue<T>(string key, T defaultValue = default(T))
        {
            T result = defaultValue;

            try
            {
                var stringVal = GetPersistingSettingStringValue(key);
                if (String.IsNullOrEmpty(stringVal))
                {
                    return defaultValue;
                }

                object val;

                if (typeof(T) == typeof(string))
                {
                    val = stringVal;
                }
                else
                if (typeof(T) == typeof(bool))
                {
                    val = Convert.ToBoolean(stringVal);
                }
                else
                if (typeof(T) == typeof(int))
                {
                    val = Convert.ToInt32(stringVal);
                }
                else
                if (typeof(T) == typeof(long))
                {
                    val = Convert.ToInt64(stringVal);
                }
                else
                {
                    return defaultValue;
                }

                result = (T)Convert.ChangeType(val, typeof(T));

            }
            catch (Exception ex)
            {
                result = defaultValue;

                _loggingService.Error(ex);
            }

            return result;
        }

        protected void SavePersistingStringValue(string key, string value)
        {
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();

                    string insertQuery = "INSERT INTO Configuration (Key, Value) VALUES (@key, @value) " +
                               "ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
                    using (var command = new SqliteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@key", key);
                        command.Parameters.AddWithValue("@value", value);
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        protected void SavePersistingSettingValue<T>(string key, T value)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    SavePersistingStringValue(key, value as string);
                }
                if (typeof(T) == typeof(bool))
                {
                    SavePersistingStringValue(key, value.ToString());
                }
                if (typeof(T) == typeof(int))
                {
                    SavePersistingStringValue(key, value.ToString());
                }
                if (typeof(T) == typeof(long))
                {
                    SavePersistingStringValue(key, value.ToString());
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
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

        public int ImportChannelsFromJSON(string json)
        {
            try
            {
                var importedChannels = JsonConvert.DeserializeObject<ObservableCollection<Channel>>(json);

                var count = 0;
                foreach (var ch in importedChannels)
                {
                    if (!ch.ChannelExists(Channels))
                    {
                        count++;
                        ch.Number = Channel.GetNextChannelNumber(Channels).ToString();
                        Channels.Add(ch);
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Import failed");
                return -1;
            }
        }

        private string ChannelsConfigFileName
        {
            get
            {
                return Path.Join(ConfigDirectory, "DVBTTelevizor.MAUI.channels.json");
            }
        }

        public void Load()
        {
            try
            {
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
                        Channels.Clear();

                        foreach (var channel in loadedChannels)
                        {
                            Channels.Add(channel.Clone());
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public void Save()
        {
            try
            {
                _loggingService.Info("Saving channels");

                var json = JsonConvert.SerializeObject(Channels);

                SavePersistingSettingValue<string>("ChannelsJson", JsonConvert.SerializeObject(Channels));

                File.WriteAllText(ChannelsConfigFileName, json);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }
    }
}
