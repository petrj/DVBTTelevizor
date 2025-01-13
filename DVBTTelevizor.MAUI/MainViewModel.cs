using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DVBTTelevizor.MAUI
{
    public class MainViewModel : BaseNotifableObject
    {
        //public event PropertyChangedEventHandler? PropertyChanged = null;
        public ObservableCollection<DVBTChannel> Channels { get; set; } = new ObservableCollection<DVBTChannel>()
        {
           new DVBTChannel()
           {
                Number = "1",
                Frequency = 1,
                Name = "Channel",
                Type = MPEGTS.ServiceTypeEnum.DigitalTelevisionService,
                ServiceType = DVBTDriverServiceType.TV
           }
        };

        private ILoggingService _loggingService;
        private IDVBTDriver _driver;
        private bool _driverInstalled = false;
        private LibVLC? LibVLC { get; set; }
        private MediaPlayer? _mediaPlayer;

        public bool InstallDriverButtonVisible
        {
            get
            {
                return _driver == null || !_driverInstalled;
            }
        }

        public MainViewModel(ILoggingService loggingService, IDVBTDriver driver)
        {
            _loggingService = loggingService;
            _driver = driver;

            InitializeVLC();

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

        public void UpdateDriverState()
        {
            OnPropertyChanged(nameof(DriverIconImage));
            OnPropertyChanged(nameof(InstallDriverButtonVisible));
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

            WeakReferenceMessenger.Default.Send(new ToastMessage($"Device found: {config.DeviceName}"));

            _driver.Configuration = config;
            _driver.Connect();

            UpdateDriverState();
        }

        private void ConnectDriverFailed(string message)
        {
            _loggingService.Info($"Connection failed: {message}");

            _driverInstalled = true;

            WeakReferenceMessenger.Default.Send(new ToastMessage($"Connection failed: {message}"));

            UpdateDriverState();
        }

        private void DriverNotInstalled()
        {
            _loggingService.Info($"Driver is not installed");

            _driverInstalled = false;

            WeakReferenceMessenger.Default.Send(new ToastMessage("DVBT driver is not installed"));

            UpdateDriverState();
        }

        public string DriverIconImage
        {
            get
            {
                if (_driver == null ||
                    !_driverInstalled ||
                    !_driver.Connected)
                {
                    return "disconnected.png";
                }

                return "connected.png";
            }
        }

        public bool DriverInstalled
        {
            get { return _driverInstalled; }
        }

        public bool MainLayoutVisible { get; set; } = true;

        public LibVLCSharp.Shared.MediaPlayer MediaPlayer
        {
            get => _mediaPlayer;
        }

        private bool IsLoaded { get; set; }
        private bool IsVideoViewInitialized { get; set; }

        private void InitializeVLC()
        {
            _loggingService.Info("Initializing LibVLC");

            LibVLC = new LibVLC(enableDebugLogs: true);
            using var media = new Media(LibVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));

            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(LibVLC)
            {
                Media = media
            };

            OnPropertyChanged(nameof(MediaPlayer));
        }

        public void OnAppearing()
        {
            _loggingService.Info("OnAppearing");

            IsLoaded = true;
            Play();
        }

        internal void OnDisappearing()
        {
            _loggingService.Info("OnDisAppearing");

            MediaPlayer.Dispose();
            LibVLC.Dispose();
        }

        public void OnVideoViewInitialized()
        {
            IsVideoViewInitialized = true;
            Play();
        }

        private void Play()
        {
            _loggingService.Info("Play");

            if (IsLoaded && IsVideoViewInitialized)
            {
                MediaPlayer.Play();
            }
        }
    }
}
