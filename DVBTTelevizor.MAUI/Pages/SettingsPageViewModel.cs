using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
using LibVLCSharp.Shared;
using LoggerService;
using Newtonsoft.Json;
using SledovaniTV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class SettingsPageViewModel : BaseViewModel
    {
        private const string DefaultLanguage = "English (default)";
        private int? previousDVBTDriverTypeindex = null;
        private bool _requestWriteToSDCardDisabled = false;
        private SledovaniTV.SledovaniTV? _iptv;

        public string IgnoreLanguageChangeNotify { get; set; } = null;

        private Quality? _selectedStreamQuality = null;

        public ObservableCollection<Channel> AutoPlayChannels { get; set; } = new ObservableCollection<Channel>();
        public ObservableCollection<string> DVBTDrivers { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> FontSizes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Languages { get; set; } = new ObservableCollection<string>();

        public ObservableCollection<Quality> Resolutions { get; set; } = new ObservableCollection<Quality>();

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


            WeakReferenceMessenger.Default.Register<SledovaniTVStateChangedMessage>(this, (r, m) =>
            {
                _iptv = m.Value;
                UpdateStreamQualities();
                NotifySledovaniTVChange();
            });
        }

        public void UpdateStreamQualities()
        {
            Task.Run(async () =>
            {
                Resolutions.Clear();

                var autoQuality = new Quality() { Name = "Auto", Id = string.Empty };
                _selectedStreamQuality = autoQuality;

                Resolutions.Add(autoQuality);

                if (_iptv != null && _iptv.Status == StatusEnum.Logged)
                {
                    var qualities = await _iptv.GetStreamQualities();
                    foreach (var q in qualities)
                    {
                        if (q.Id == _configuration.SledovaniTVResolution)
                        {
                            _selectedStreamQuality = q;
                        }
                        Resolutions.Add(q);
                    }
                }
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    OnPropertyChanged(nameof(Resolutions));
                    OnPropertyChanged(nameof(SelectedStreamQuality));
                });
            });
        }

        public Quality SelectedStreamQuality
        {
            get
            {
                return _selectedStreamQuality;
            }
            set
            {
                if (value == null)
                    return;
                _selectedStreamQuality = value;
                _configuration.SledovaniTVResolution = value?.Id?? string.Empty;
                OnPropertyChanged(nameof(SelectedStreamQuality));
            }
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
                OnPropertyChanged(nameof(EnableLogging));
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

        public bool EnableLogging
        {
            get
            {
                return _configuration.EnableLogging;
            } set
            {
                _configuration.EnableLogging = value;

                NotifyConfigChange();
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

            FontSizes.Add("Smallest".Translated());
            FontSizes.Add("Very small".Translated());
            FontSizes.Add("Smaller".Translated());
            FontSizes.Add("Normal".Translated());
            FontSizes.Add("Above normal".Translated());
            FontSizes.Add("Big".Translated());
            FontSizes.Add("Bigger".Translated());
            FontSizes.Add("Very big".Translated());
            FontSizes.Add("Huge".Translated());
            FontSizes.Add("Huge+".Translated());
            FontSizes.Add("Huge++".Translated());
            FontSizes.Add("Colossal".Translated());

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

        // offset converts between negative enum values (-3..8) and list indices (0..11)
        private static readonly int _fontSizeOffset = -(int)AppFontSizeEnum.Smallest;

        public int AppFontSizeIndex
        {
            get
            {
                return (int)_configuration.AppFontSize + _fontSizeOffset;
            }
            set
            {
                _configuration.AppFontSize = (AppFontSizeEnum)(value - _fontSizeOffset);

                OnPropertyChanged(nameof(AppFontSizeIndex));
                WeakReferenceMessenger.Default.Send(new FontSizeChangedMessage((int)_configuration.AppFontSize));
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
                return _iptv != null && _iptv.Paired && Config.SledovaniTVShowAdultChannels;
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
                return (_iptv != null && _iptv.Paired);
            }
        }

        public bool SledovaniTVDeviceNotPaired
        {
            get
            {
                return  (_iptv == null || !_iptv.Paired);
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



        public async Task ExportSettings()
        {
            _loggingService.Info("ExportSettings");

            try
            {
                // Create a DummyConfiguration from the current configuration
                var dummyConfig = DummyConfiguration.FromConfiguration(_configuration);

                var settingsJSON = Newtonsoft.Json.JsonConvert.SerializeObject(dummyConfig, Newtonsoft.Json.Formatting.Indented);

                _loggingService.Info($"Serialized settings JSON: {settingsJSON}");

                if (File.Exists(AndroidSettingsListPath))
                {
                    File.Delete(AndroidChannelsListPath);
                }
                System.IO.File.WriteAllText(AndroidSettingsListPath, settingsJSON);

                _loggingService.Info($"Settings exported to: {AndroidSettingsListPath}");

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

                cfg?.UpdateConfig(_configuration);

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

        public bool AllowRemoteVLC
        {
            get
            {
                return _configuration.AllowRemoteVLC;
            }
            set
            {
                _configuration.AllowRemoteVLC = value;

                OnPropertyChanged(nameof(AllowRemoteVLC));
            }
        }


    }
}

