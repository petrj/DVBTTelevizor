using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<Channel> AutoPlayChannels { get; set; } = new ObservableCollection<Channel>();
        public ObservableCollection<string> DVBTDrivers { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> FontSizes { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> Languages { get; set; } = new ObservableCollection<string>();

        public Channel _selectedChannel = null;
        private bool _menuVisible = false;

        public SettingsPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
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

        public bool MenuVisible
        {
            get
            {
                return _menuVisible;
            }
            set
            {
                _menuVisible = value;

                OnPropertyChanged(nameof(MenuVisible));
            }
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

                if (value != null)
                    Config.AutoPlayedChannelFrequencyAndMapPID = value.FrequencyAndMapPID;

                OnPropertyChanged(nameof(SelectedChannel));
            }
        }

        public async void FillDVBTDrivers()
        {
            DVBTDrivers.Clear();

            DVBTDrivers.Add("DVBT - Android".Translated());
            DVBTDrivers.Add("DVBT - Android - Test".Translated());
            DVBTDrivers.Add("DVBT - Test".Translated());
            DVBTDrivers.Add("FM - Android".Translated());
            DVBTDrivers.Add("FM".Translated());

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(DVBTDrivers));
                OnPropertyChanged(nameof(DVBTDriverTypeIndex));
            });
        }

        public async void FillLanguages()
        {
            Languages.Clear();

            Languages.Add(DefaultLanguage);

            foreach (var lng in Lng.Languages)
            {
                Languages.Add(lng);
            }

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
                Frequency = -1,
                ProgramMapPID = -1
            };
            var lastChannel = new Channel()
            {
                Name = "<last channel>".Translated(),
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
                AutoPlayChannels.Add(ch.Clone());

                if (ch.FrequencyAndMapPID == Config.AutoPlayedChannelFrequencyAndMapPID)
                {
                    anythingSelected = true;
                    SelectedChannel = ch;
                }
            }

            if (!anythingSelected && (!string.IsNullOrEmpty(Config.AutoPlayedChannelFrequencyAndMapPID)))
            {
                if (Config.AutoPlayedChannelFrequencyAndMapPID == noChannel.FrequencyAndMapPID)
                {
                    SelectedChannel = noChannel;
                }
                else
                {
                    SelectedChannel = lastChannel;
                }
            }
            else
            {
                SelectedChannel = noChannel;
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

        public int DVBTDriverTypeIndex
        {
            get
            {
                return (int)_configuration.DVBTDriverType;
            }
            set
            {
                _configuration.DVBTDriverType = (DVBTDriverTypeEnum)value;
                OnPropertyChanged(nameof(DVBTDriverTypeIndex));

                if ((previousDVBTDriverTypeindex != null) &&
                    previousDVBTDriverTypeindex != value)
                {
                    WeakReferenceMessenger.Default.Send(new DVBTDriverChangedMessage(String.Empty));
                }

                previousDVBTDriverTypeindex = value;

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
            }
        }

        public string AndroidChannelsListPath
        {
            get
            {
                return $"{Config.OutputDirectory}{System.IO.Path.DirectorySeparatorChar}DVBTTelevizor.channels.json";
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
    }
}

