using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LibVLCSharp.Shared;
using LoggerService;
using System.ComponentModel;

namespace DVBTTelevizor.MAUI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged = null;
        private ILoggingService _loggingService;
        private IDVBTDriver _driver;
        private bool _driverInstalled = false;
        private LibVLC? LibVLC { get; set; }
        private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;

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

        private void ConnectDriver(DVBTDriverConfiguration config)
        {
            _loggingService.Info("Connecting device: " + config.DeviceName);

            _driverInstalled = true;

            WeakReferenceMessenger.Default.Send(new ToastMessage($"Device found: {config.DeviceName}"));

            _driver.Configuration = config;
            _driver.Connect();

            //OnPropertyChanged(nameof(DriverIconImage));
        }

        private void ConnectDriverFailed(string message)
        {
            _loggingService.Info($"Connection failed: {message}");

            _driverInstalled = true;

            WeakReferenceMessenger.Default.Send(new ToastMessage($"Connection failed: {message}"));
        }

        private void DriverNotInstalled()
        {
            _loggingService.Info($"Driver is not installed");

            _driverInstalled = false;

            WeakReferenceMessenger.Default.Send(new ToastMessage("DVBT driver is not installed"));
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
            private set => Set(nameof(MediaPlayer), ref _mediaPlayer, value);
        }

        private bool IsLoaded { get; set; }
        private bool IsVideoViewInitialized { get; set; }

        private void Set<T>(string propertyName, ref T field, T value)
        {
            if (field == null && value != null || field != null && !field.Equals(value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private void InitializeVLC()
        {
            _loggingService.Info("Initializing LibVLC");

            LibVLC = new LibVLC(enableDebugLogs: true);
            using var media = new Media(LibVLC, new Uri("http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));

            MediaPlayer = new LibVLCSharp.Shared.MediaPlayer(LibVLC)
            {
                Media = media
            };
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
