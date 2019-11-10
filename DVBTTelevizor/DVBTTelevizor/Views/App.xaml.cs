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

        public App(ILoggingService loggingService)
        {
            InitializeComponent();

            _loggingService = loggingService;

            _mainPage = new MainPage(_loggingService);
            MainPage = new NavigationPage(_mainPage);
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            _loggingService.Info($"OnSleep");

            _mainPage.StopPLayback();
        }


        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
