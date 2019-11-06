using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.IO;
using System.Threading;
using LoggerService;

namespace DVBTTelevizor
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private MainPageViewModel _viewModel;

        DVBTDriverManager _driver;
        DialogService _dlgService;
        ILoggingService _loggingService;
        private DVBTTelevizorConfiguration _config;
        PlayerPage _playerPage;
        ServicePage _servicePage;
        TunePage _tunePage;

        public MainPage(ILoggingService loggingService)
        {
            InitializeComponent();

            _dlgService = new DialogService(this);

            _loggingService = loggingService;

            _config = new DVBTTelevizorConfiguration()
            {
                AutoInitAfterStart = true
            };
            
            _driver = new DVBTDriverManager(_loggingService, _config);

            try
            {
                _playerPage = new PlayerPage(_driver);
            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while initializing player page");
            }

            _tunePage = new TunePage(_loggingService, _dlgService, _driver, _config);
            _servicePage = new ServicePage(_loggingService, _dlgService,_driver,_config, _playerPage);

            BindingContext = _viewModel = new MainPageViewModel(_loggingService, _dlgService, _driver, _config);

            _servicePage.Disappearing += delegate
             {
                 _viewModel.RefreshCommand.Execute(null);
             };
            _tunePage.Disappearing += delegate
            {
                _viewModel.RefreshCommand.Execute(null);
            };            

            MessagingCenter.Subscribe<string>(this, "PlayStream", (message) =>
            {
                Device.BeginInvokeOnMainThread(
                 new Action(() =>
                {
                if (_playerPage != null)
                {
                    Navigation.PushModalAsync(_playerPage);
                } else
                {
                    Task.Run(async() =>
                            {
                                await _dlgService.Error("Player not initialized");
                            }
                       );                         
                     }
                 }));
            });


            if (_config.AutoInitAfterStart)
            {
                Task.Run( () =>
               {
                   Xamarin.Forms.Device.BeginInvokeOnMainThread(
                   new Action(
                   delegate
                   {
                       MessagingCenter.Send("", "Init");
                   }));
               });
            }            
        }

        private void ToolbarServicePage_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(_servicePage);
        }

        private void ToolbarTune_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(_tunePage);
        }

        private void ToolbarRefresh_Clicked(object sender, EventArgs e)
        {
            _viewModel.RefreshCommand.Execute(null);
        }


    }
}
