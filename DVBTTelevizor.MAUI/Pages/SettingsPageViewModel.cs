using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LibVLCSharp.Shared;
using LoggerService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class SettingsPageViewModel : BaseViewModel
    {
        private const string DefaultLanguage = "English (default)";
        private int? previousDVBTDriverTypeindex = null;
        private bool _requestWriteToSDCardDisabled = false;
        private SledovaniTV.SledovaniTV _iptv;

        public string IgnoreLanguageChangeNotify { get; set; } = null;

        public ObservableCollection<Channel> AutoPlayChannels { get; set; } = new ObservableCollection<Channel>();
        public ObservableCollection<string> DVBTDrivers { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> FontSizes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Languages { get; set; } = new ObservableCollection<string>();

        public Channel _selectedChannel = null;
        private bool _showPairedDevice = false;

        public SettingsPageViewModel(ILoggingService loggingService, IDriverConnector driver, SledovaniTV.SledovaniTV iptv, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            _iptv = iptv;

            WeakReferenceMessenger.Default.Register<ExternalDeviceWriteAccessGranted>(this, (r, m) =>
            {
                AllowWriteToSDCard(m?.Value?.Path, m?.Value?.PathUri);
            });
        }

        public void AllowWriteToSDCard(string path, string pathUri)
        {
            _loggingService.Info($"AllowWriteToSDCard: {path}");

            try
            {
                _requestWriteToSDCardDisabled = true;

                Config.WriteToExternalDevice = true;
                Config.ExternalDevicePath = path; // Android 11+ stores path to SD Card folder here
                Config.ExternalDevicePathUri = pathUri;

                NotifyConfigChange();
            }
            finally
            {
                _requestWriteToSDCardDisabled = false;
            }
        }

        public async void NotifyConfigChange()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                OnPropertyChanged(nameof(Config));
            });
        }

        public async void NotifyLanguageChange()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                OnPropertyChanged(nameof(Languages));
                OnPropertyChanged(nameof(SelectedLanguage));
            });
        }

        public void RequestWriteToSDCard()
        {
            if (_requestWriteToSDCardDisabled)
                return;

            _loggingService.Info("RequestWriteToSDCard");

            // automatically diable write to sd card until permissions granted
            Config.WriteToExternalDevice = false;

            // check SD card permissions
            WeakReferenceMessenger.Default.Send(new ExternalDeviceWriteRequestMessage(String.Empty));
        }

        public string SelectedLanguage
        {
            get
            {
                var language = _configuration.Language;
                if (string.IsNullOrEmpty(language))
                {
                    language = DefaultLanguage;
                }
                return language;
            }
            set
            {
                if (value == null)
                    return;

                _configuration.Language = value;

                NotifyLanguageChange();
            }
        }

        public Channel SelectedChannel
        {
            get
            {
                return _selectedChannel;
            }
            set
            {
                _selectedChannel = value;

                if ((value != null) && (Config.AutoPlayedChannelUniqueID != value.UniqueIdentifier))
                {
                    Config.AutoPlayedChannelUniqueID = value.UniqueIdentifier;
                }

                OnPropertyChanged(nameof(SelectedChannel));
            }
        }

        public async void FillLanguages()
        {
            Languages.Clear();

            Languages.Add(DefaultLanguage);

            foreach (var lng in Lng.Languages)
            {
                Languages.Add(lng);
            }

            IgnoreLanguageChangeNotify = SelectedLanguage;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(Languages));
                OnPropertyChanged(nameof(SelectedLanguage));
            });
        }

        public async void FillFontSizes()
        {
            FontSizes.Clear();

            FontSizes.Add("Normal".Translated());
            FontSizes.Add("Above normal".Translated());
            FontSizes.Add("Big".Translated());
            FontSizes.Add("Bigger".Translated());
            FontSizes.Add("Very big".Translated());
            FontSizes.Add("Huge".Translated());
            FontSizes.Add("Huge".Translated() + " +");
            FontSizes.Add("Huge".Translated() + " +++");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(FontSizes));
                OnPropertyChanged(nameof(AppFontSizeIndex));
            });
        }

        public async void FillAutoPlayChannels()
        {
            AutoPlayChannels.Clear();

            var noChannel = new Channel()
            {
                Name = "<no channel>".Translated(),
                ChannelId = "",
                Frequency = 0,
                ProgramMapPID = 0
            };
            var lastChannel = new Channel()
            {
                Name = "<last channel>".Translated(),
                ChannelId = "last",
                Frequency = 0,
                ProgramMapPID = 0
            };

            var channels = _configuration.GetChannels();

            if (channels.Count == 0)
            {
                SelectedChannel = noChannel;
                return;
            }

            AutoPlayChannels.Add(noChannel);
            AutoPlayChannels.Add(lastChannel);

            var anythingSelected = false;

            foreach (var ch in channels)
            {
                var clonedChannel = ch.Clone();
                AutoPlayChannels.Add(clonedChannel);

                if (clonedChannel.UniqueIdentifier == Config.AutoPlayedChannelUniqueID)
                {
                    anythingSelected = true;
                    SelectedChannel = clonedChannel;
                }
            }

            if (!anythingSelected && (!string.IsNullOrEmpty(Config.AutoPlayedChannelUniqueID)))
            {
                if (Config.AutoPlayedChannelUniqueID == noChannel.UniqueIdentifier)
                {
                    SelectedChannel = noChannel;
                }
                else if (Config.AutoPlayedChannelUniqueID == lastChannel.UniqueIdentifier)
                {
                    SelectedChannel = lastChannel;
                }
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(AutoPlayChannels));
                OnPropertyChanged(nameof(SelectedChannel));
            });
        }

        public int AppFontSizeIndex
        {
            get
            {
                return (int)_configuration.AppFontSize;
            }
            set
            {
                _configuration.AppFontSize = (AppFontSizeEnum)value;

                OnPropertyChanged(nameof(AppFontSizeIndex));
                WeakReferenceMessenger.Default.Send(new FontSizeChangedMessage(AppFontSizeIndex));
            }
        }

        public bool AllowRemoteAccessService
        {
            get
            {
                return Config.AllowRemoteAccessService;
            }
            set
            {
                Config.AllowRemoteAccessService = value;

                OnPropertyChanged(nameof(Config));
                OnPropertyChanged(nameof(AllowRemoteAccessService));
            }
        }

        public string AndroidChannelsListPath
        {
            get
            {
                return $"{Config.ConfigDirectory}{System.IO.Path.DirectorySeparatorChar}DVBTTelevizor.channels.json";
            }
        }

        public string AndroidSettingsListPath
        {
            get
            {
                return $"{Config.ConfigDirectory}{System.IO.Path.DirectorySeparatorChar}DVBTTelevizor.configuration.json";
            }
        }

        public bool SledovaniTVEnabled
        {
            get
            {
                return Config.SledovaniTVEnabled;
            }
            set
            {
                Config.SledovaniTVEnabled = value;

                NotifySledovaniTVChange();
            }
        }

        public bool SledovaniTVShowAdultChannels
        {
            get
            {
                return Config.SledovaniTVShowAdultChannels;
            }
            set
            {
                Config.SledovaniTVShowAdultChannels = value;

                NotifySledovaniTVChange();
            }
        }

        public bool SledovaniTVShowPairedDevice
        {
            get
            {
                return SledovaniTVDevicePaired && _showPairedDevice;
            }
            set
            {
                _showPairedDevice = value;
                NotifySledovaniTVChange();
            }
        }

        public bool SledovaniTVDevicePaired
        {
            get
            {
                return _iptv.Paired;
            }
        }

        public bool SledovaniTVDeviceNotPaired
        {
            get
            {
                return _iptv.NotPaired;
            }
        }

        public void NotifySledovaniTVChange()
        {

            OnPropertyChanged(nameof(Config));
            OnPropertyChanged(nameof(SledovaniTVShowAdultChannels));
            OnPropertyChanged(nameof(SledovaniTVShowPairedDevice));
            OnPropertyChanged(nameof(SledovaniTVDevicePaired));
            OnPropertyChanged(nameof(SledovaniTVDeviceNotPaired));
            OnPropertyChanged(nameof(SledovaniTVEnabled));
        }

        public async Task SledovaniTVPair()
        {
            _loggingService.Info("SledovaniTVPair");

            try
            {
                _iptv.SetCredentials(_configuration.SledovaniTVUserName, _configuration.SledovaniTVPassword, _configuration.SledovaniTVPIN);
                _iptv.SetDeviceCredential(_configuration.SledovaniTVDeviceID, _configuration.SledovaniTVDevicePassword);
                await _iptv.Login();
                _configuration.SledovaniTVDeviceID = _iptv.Connection.deviceId;
                _configuration.SledovaniTVDevicePassword = _iptv.Connection.password;

                NotifySledovaniTVChange();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public async Task SledovaniTVReloadChannels()
        {
            _loggingService.Info("SledovaniTVReloadChannels");

            try
            {
                var iptvChannels = await _iptv.GetChannels();
                var channels = _configuration.GetChannels();

                var countImported = 0;
                var countUpdated = 0;

                foreach (var iptvChannel in iptvChannels)
                {
                    var found = false;

                    // searching for online channel with the same id
                    foreach (var channel in channels)
                    {
                        if ((channel.ChannelType == ChannelTypeEnum.SledovaniTV) && (channel.ChannelId == iptvChannel.ChannelId))
                        {
                            // update
                            found = true;

                            channel.Url = iptvChannel.Url;
                            channel.IconUrl = iptvChannel.IconUrl;
                            channel.Name = iptvChannel.Name;
                            channel.ProviderName = "SledovaniTV";

                            countUpdated++;

                            break;
                        }
                    }

                    if (!found)
                    {
                        // create
                        var num = TuningProgressPageViewModel.GetNextFreeChannelNumber(channels);
                        var ch = new Channel()
                        {
                            Number = num,
                            ChannelId = iptvChannel.ChannelId,
                            Url = iptvChannel.Url,
                            IconUrl = iptvChannel.IconUrl,
                            Name = iptvChannel.Name,
                            ProviderName = "SledovaniTV",
                            Type = iptvChannel.Type,
                            ChannelType = ChannelTypeEnum.SledovaniTV
                        };

                        channels.Add(ch);
                        countImported++;
                    }
                }

                _configuration.SaveChannels(channels);

                WeakReferenceMessenger.Default.Send(new ChannelsChangedMessage(String.Empty));
                WeakReferenceMessenger.Default.Send(new ToastMessage("{0} channels updated, {1} added".Translated(countUpdated.ToString(), countImported.ToString())));

                NotifySledovaniTVChange();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public async Task ExportSettings()
        {
            _loggingService.Info("ExportSettings");

            try
            {
                var settingsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(_configuration, Newtonsoft.Json.Formatting.Indented);

                if (File.Exists(AndroidSettingsListPath))
                {
                    File.Delete(AndroidChannelsListPath);
                }
                System.IO.File.WriteAllText(AndroidSettingsListPath, settingsJSON);

                WeakReferenceMessenger.Default.Send(new ToastMessage("Settings exported".Translated()));

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        public async Task ImportSettings()
        {
            _loggingService.Info("ImportSettings");

            try
            {
                var json = File.ReadAllText(AndroidSettingsListPath);
                var cfg = JsonConvert.DeserializeObject<DummyConfiguration>(json);

                cfg.UpdateConfig(_configuration);

                _iptv.SetCredentials(_configuration.SledovaniTVUserName, _configuration.SledovaniTVPassword, _configuration.SledovaniTVPIN);
                _iptv.SetDeviceCredential(_configuration.SledovaniTVDeviceID, _configuration.SledovaniTVDevicePassword);

                NotifyConfigChange();
                NotifyLanguageChange();
                NotifySledovaniTVChange();

                WeakReferenceMessenger.Default.Send(new ToastMessage("Settings imported".Translated()));

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                WeakReferenceMessenger.Default.Send(new ToastMessage("Settings import error".Translated()));
            }
        }

        public bool DebugSettingsVisible
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public bool AllowRemoteSDR
        {
            get
            {
                return _configuration.AllowRemoteSDR;
            }
            set
            {
                _configuration.AllowRemoteSDR = value;

                OnPropertyChanged(nameof(AllowRemoteSDR));
            }
        }


    }
}

