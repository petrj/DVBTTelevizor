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
        private ILoggingService? _loggingService = null;
        IDVBTDriver? _driver = null;
        private bool _driverInstalled = false;

        public MainViewModel(ILoggingService loggingService, IDVBTDriver driver)
        {
            _loggingService = loggingService;
            _driver = driver;

            InitializeVLC();

            WeakReferenceMessenger.Default.Register<DVBTDriverConnectionFailedMessage>(this, (r, m) =>
            {
                WeakReferenceMessenger.Default.Send(new ToastMessage(m.Value));
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverNotInstalledMessage>(this, (r, m) =>
            {
                WeakReferenceMessenger.Default.Send(new ToastMessage("DVBT driver is not installed"));
            });
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

        private LibVLC LibVLC { get; set; }

        public bool MainLayoutVisible { get; set; } = true;

        private LibVLCSharp.Shared.MediaPlayer _mediaPlayer;

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
