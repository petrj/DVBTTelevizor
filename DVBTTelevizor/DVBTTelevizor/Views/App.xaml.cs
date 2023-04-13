using LoggerService;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    public partial class App : Application
    {
        MainPage _mainPage;
        ILoggingService _loggingService;
        DVBTTelevizorConfiguration _config;
        IDVBTDriverManager _driver;

        public App(ILoggingService loggingService, DVBTTelevizorConfiguration config, IDVBTDriverManager driverManager)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _config = config;
            _driver = driverManager;

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
                Task.Run(async () =>
                {
                    await _mainPage.ActionStop(true);
                });
            }
        }

        protected override async void OnResume()
        {
            _loggingService.Info($"OnResume");

            if (_config.PlayOnBackground)
            {
                _mainPage.ResumePlayback();
            }

            if (!_driver.Started)
            {
                MessagingCenter.Send("", BaseViewModel.MSG_Init);
            }
            else
            {
                var status = await _driver.CheckStatus();

                if (!status)
                {
                    await _driver.Disconnect();

                    MessagingCenter.Send("connection failed", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                }
            }
        }

        public void Done()
        {
            _mainPage.Done();
        }
    }
}
