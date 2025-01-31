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
        public ObservableCollection<Channel> AutoPlayChannels { get; set; } = new ObservableCollection<Channel>();
        public ObservableCollection<string> DVBTDrivers { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> FontSizes { get; set; } = new ObservableCollection<string>();

        public Channel _selectedChannel = null;

        public SettingsPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
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

            DVBTDrivers.Add("Android DVBT driver".Translated());
            DVBTDrivers.Add("Android testing DVBT Driver".Translated());
            DVBTDrivers.Add("Test tune driver".Translated());

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(DVBTDrivers));
                OnPropertyChanged(nameof(DVBTDriverTypeIndex));
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

            if (_configuration.Channels.Count == 0)
            {
                SelectedChannel = noChannel;
                return;
            }

            AutoPlayChannels.Add(noChannel);
            AutoPlayChannels.Add(lastChannel);

            var anythingSelected = false;

            foreach (var ch in _configuration.Channels)
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
                NotifyFontSizeChange();
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
    }
}

