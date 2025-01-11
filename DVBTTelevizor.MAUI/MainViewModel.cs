using LibVLCSharp.Shared;
using LoggerService;
using System.ComponentModel;

namespace DVBTTelevizor.MAUI
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private ILoggingService _loggingService;

        public MainViewModel(ILoggingService loggingService)
        {
            _loggingService = loggingService;

            Initialize();
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

        private void Initialize()
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
