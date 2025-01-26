using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.DBManager;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace DVBTTelevizor.MAUI
{
    public class MainViewModel : BaseViewModel
    {
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private PlayingStateEnum _playingState = PlayingStateEnum.Stopped;

        //public event PropertyChangedEventHandler? PropertyChanged = null;
        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();

        public Dictionary<int, string> PlayingChannelSubtitles { get; set; } = new Dictionary<int, string>();
        public Dictionary<int, string> PlayingChannelAudioTracks { get; set; } = new Dictionary<int, string>();
        public Size PlayingChannelAspect { get; set; } = new Size(-1, -1);

        private bool _EPGDetailEnabled = true;

        private bool _driverInstalled = false;

        private Channel _selectedChannel;
        private Channel _playingChannel;
        private Channel _recordingChannel;

        public EITManager EIT { get; set; }
        public PIDManager PID { get; set; }

        public MainViewModel(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
            :base(loggingService,driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
            EIT = new EITManager(loggingService, publicDirectoryProvider, driver);
            PID = new PIDManager(loggingService, publicDirectoryProvider, driver);

            WeakReferenceMessenger.Default.Register<DVBTDriverConnectedMessage>(this, (r, m) =>
            {
                ConnectDriver(m.Value);
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverConnectionFailedMessage>(this, (r, m) =>
            {
                ConnectDriverFailed(m.Value);
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverNotInstalledMessage>(this, (r, m) =>
            {
                DriverNotInstalled();
            });
        }

        public async Task RefreshChannels()
        {
            _loggingService.Debug($"Refreshing EPG");

            try
            {
                await _semaphoreSlim.WaitAsync();

                _configuration.Load();
                NotifyFontSizeChange();

                Channels.Clear();
                foreach (var channel in _configuration.Channels)
                {
                    Channels.Add(channel.Clone());
                }

                OnPropertyChanged(nameof(Channels));
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Refreshing EPG failed");
            }
            finally
            {
                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(SelectedChannel));
                OnPropertyChanged(nameof(SelectedChannelEPGTitle));
                OnPropertyChanged(nameof(SelectedChannelEPGDescription));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
                OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
                OnPropertyChanged(nameof(SelectedChannelEPGProgress));
                OnPropertyChanged(nameof(EPGProgressBackgroundColor));
                //NotifyEPGDetailVisibilityChange();
            }
        }

        public async Task Import(string filename)
        {
            try
            {
                _loggingService.Info($"Importing channels from file");

                if (!File.Exists(filename))
                {
                    await _dialogService.Information("File {0} not found".Translated(filename));
                    return;
                }

                var count = _configuration.ImportChannelsFromJSON(File.ReadAllText(filename));
                _configuration.Save();

                await RefreshChannels();

                WeakReferenceMessenger.Default.Send(new ToastMessage("Imported channels count: {0}".Translated(count.ToString())));
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Import failed");
                await _dialogService.Information("Import failed".Translated());
            }
        }

        public bool EPGDetailEnabled
        {
            get
            {
                return _EPGDetailEnabled;
            }
            set
            {
                _EPGDetailEnabled = value;
                OnPropertyChanged(nameof(EPGDetailVisible));
                //NotifyEPGDetailVisibilityChange();
            }
        }

        public bool EPGDetailVisible
        {
            get
            {
                return true;
                // TODO: remove after debugged!

                /*
                return
                    EPGDetailEnabled &&
                    SelectedChannel != null &&
                    SelectedChannel.CurrentEventItem != null;
                */
            }
        }

        public bool InstallDriverButtonVisible
        {
            get
            {
                return _driver == null || !_driverInstalled;
            }
        }

        public void UpdateDriverState()
        {
            OnPropertyChanged(nameof(DriverIconImage));
            OnPropertyChanged(nameof(InstallDriverButtonVisible));
        }

        public async Task ShowActualPlayingMessage(PlayStreamInfo playStreamInfo = null)
        {
            if (playStreamInfo == null ||
                playStreamInfo.Channel == null)
            {
                if (SelectedChannel == null)
                    return;

                playStreamInfo = new PlayStreamInfo
                {
                    Channel = SelectedChannel
                };

                //playStreamInfo.CurrentEvent = await GetChannelEPG(SelectedChannel);
            }

            var msg = playStreamInfo.ShortInfoWithoutChannelName ? "" : " \u25B6 " + playStreamInfo.Channel.Name;

            if (playStreamInfo.CurrentEvent != null && playStreamInfo.CurrentEvent.CurrentEventItem != null)
            {
                if (msg != "")
                {
                    msg += " - ";
                }
                msg += $"{playStreamInfo.CurrentEvent.CurrentEventItem.EventName}";
            }

            // showing signal percents only for the first time
            if (playStreamInfo.SignalStrengthPercentage > 0)
            {
                msg += Environment.NewLine + "(signal {0}%)".Translated(playStreamInfo.SignalStrengthPercentage.ToString());
                playStreamInfo.SignalStrengthPercentage = 0;
            }

            WeakReferenceMessenger.Default.Send(new ToastMessage(msg));
        }

        public void NotifyChannelChange()
        {
            OnPropertyChanged(nameof(SelectedChannel));
            OnPropertyChanged(nameof(NoVideoTitle));
            OnPropertyChanged(nameof(SelectedChannelEPGTitle));
            OnPropertyChanged(nameof(SelectedChannelEPGDescription));
            OnPropertyChanged(nameof(SelectedChannelEPGTimeStart));
            OnPropertyChanged(nameof(SelectedChannelEPGTimeFinish));
            OnPropertyChanged(nameof(SelectedChannelEPGProgress));
            OnPropertyChanged(nameof(EPGProgressBackgroundColor));
            OnPropertyChanged(nameof(RecordingLabel));
        }

        public async void DisconnectDriver()
        {
            await _driver.Disconnect();

            UpdateDriverState();
        }

        private void ConnectDriver(DVBTDriverConfiguration config)
        {
            _loggingService.Info("Connecting device: " + config.DeviceName);

            _driverInstalled = true;

            WeakReferenceMessenger.Default.Send(new ToastMessage("Device found: {0}".Translated(config.DeviceName)));

            _driver.Configuration = config;
            _driver.PublicDirectory = _publicDirectory;
            _driver.Connect();

            UpdateDriverState();
        }

        private void ConnectDriverFailed(string message)
        {
            _loggingService.Info($"Connection failed: {message}");

            _driverInstalled = true;

            WeakReferenceMessenger.Default.Send(new ToastMessage("Connection failed: {0}".Translated(message)));

            UpdateDriverState();
        }

        private void DriverNotInstalled()
        {
            _loggingService.Info($"Driver is not installed");

            _driverInstalled = false;

            WeakReferenceMessenger.Default.Send(new ToastMessage("DVBT driver is not installed".Translated()));

            UpdateDriverState();
        }

        public PlayingStateEnum PlayingState
        {
            get
            {
                return _playingState;
            }
            set
            {
                _playingState = value;
            }
        }

        public string DriverIconImage
        {
            get
            {
                if (_driver == null ||
                    !_driverInstalled)
                {
                    return "uninstalled.png";
                }


                if (_driver.Connected)
                {
                    return "connected.png";

                }

                return "disconnected.png";
            }
        }

        public string TuneIconImage
        {
            get
            {
                return "tune.png";
            }
        }


        public string SettingsIconImage
        {
            get
            {
                return "settings.png";
            }
        }

        public string MenuIconImage
        {
            get
            {
                return "menu.png";
            }
        }

        public Channel SelectedChannel
        {
            get
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    return _selectedChannel;
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
            set
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    _selectedChannel = value;

                    NotifyChannelChange();
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
        }

        public bool DriverInstalled
        {
            get { return _driverInstalled; }
        }

        public bool MainLayoutVisible { get; set; } = true;

        public void OnAppearing()
        {
            _loggingService.Info("OnAppearing");
        }


        public Channel PlayingChannel
        {
            get { return _playingChannel; }
            set
            {
                _playingChannel = value;
            }
        }

        public string NoVideoTitle
        {
            get
            {
                if (PlayingChannel == null)
                {
                    if (SelectedChannel == null)
                        return null;

                    return SelectedChannel.Name;
                }
                else
                {
                    return PlayingChannel.Name;
                }
            }
        }

        public string SelectedChannelEPGTitle
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return "- no channel -";

                return SelectedChannel.CurrentEventItem.EventName;
            }
        }

        public string SelectedChannelEPGDescription
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return String.Empty;

                return SelectedChannel.CurrentEventItem.Text;
            }
        }


        public string SelectedChannelEPGTimeStart
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return String.Empty;

                return SelectedChannel.CurrentEventItem.EPGTimeStartDescription;
            }
        }

        public string SelectedChannelEPGTimeFinish
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return String.Empty;

                return SelectedChannel.CurrentEventItem.EPGTimeFinishDescription;
            }
        }

        public double SelectedChannelEPGProgress
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return 0;

                return SelectedChannel.CurrentEventItem.Progress;
            }
        }

        public Color EPGProgressBackgroundColor
        {
            get
            {
                if (SelectedChannel == null || SelectedChannel.CurrentEventItem == null)
                    return Color.FromRgba(0, 0, 0, 255);

                return Color.FromRgba(255, 255, 255, 255);
            }
        }

        public string RecordingLabel
        {
            get
            {
                if (_recordingChannel == null || _playingState == PlayingStateEnum.Stopped)
                    return string.Empty;

                return "\u25CF";
            }
        }

        public Channel RecordingChannel
        {
            get
            {
                return _recordingChannel;
            }
            set
            {
                _recordingChannel = value;

                OnPropertyChanged(nameof(RecordingLabel));
            }
        }
    }
}
