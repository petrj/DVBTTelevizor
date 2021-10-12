using LoggerService;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    public partial class App : Application
    {
        MainPage _mainPage;
        ILoggingService _loggingService;
        DVBTTelevizorConfiguration _config;

        public App(ILoggingService loggingService, DVBTTelevizorConfiguration config, DVBTDriverManager driverManager)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _config = config;

            _mainPage = new MainPage(_loggingService, config, driverManager);
            MainPage = new NavigationPage(_mainPage);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            _loggingService.Info($"OnSleep");

            if (!_config.PlayOnBackground)
            {
                _mainPage.StopPlayback();
            }
        }

        protected override void OnResume()
        {
            _loggingService.Info($"OnResume");

            if (_config.PlayOnBackground)
            {
                _mainPage.ResumePlayback();
            }
        }

        public void Done()
        {
            _mainPage.Done();
        }
    }
}
