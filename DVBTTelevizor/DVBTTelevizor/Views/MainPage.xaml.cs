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
using Android.Media;
using LibVLCSharp.Shared;
using Android.Widget;

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
        ILoggingService _log;
        private DVBTTelevizorConfiguration _config;
        PlayerPage _playerPage;
        ServicePage _servicePage;

        public MainPage()
        {
            InitializeComponent();          


            this.SaveChannelsButton.Clicked += SaveChannelsButton_Clicked;
            this.PlayButton.Clicked += PlayButton_Clicked;            

            _dlgService = new DialogService(this);
            _log = new BasicLoggingService();
            _config = new DVBTTelevizorConfiguration()
            {
                AutoInitAfterStart = true
            };

            _driver = new DVBTDriverManager(_log, _config);

            _playerPage = new PlayerPage(_driver);
            _servicePage = new ServicePage(_log,_dlgService,_driver,_config);

            _servicePage.Disappearing += delegate
             {
                 _viewModel.RefreshCommand.Execute(null);
             };

            BindingContext = _viewModel = new MainPageViewModel(_log, _dlgService, _driver, _config);

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                do
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(
                        new Action(
                            delegate
                            {
                                DataStreamInfoLabel.Text = _driver.DataStreamInfo;
                            }));

                    // 2 secs delay
                    Thread.Sleep(2 * 1000);

                } while (true);
            }).Start();

            MessagingCenter.Subscribe<string>(this, "PlayStream", (message) =>
            {
                Device.BeginInvokeOnMainThread(
                 new Action( () =>                 
                 {
                     Navigation.PushModalAsync(_playerPage);
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
            Navigation.PushModalAsync(_servicePage);
        }      


        private void PlayButton_Clicked(object sender, EventArgs e)
        {
           Navigation.PushModalAsync(_playerPage);
        }
      
        private void SaveChannelsButton_Clicked(object sender, EventArgs e)
        {
            StatusLabel.Text = "Saving channels to  configuration ...";

            Task.Run(async () =>
            {
                try
                {
                    _viewModel.SaveChannelsToConfig();
                }
                catch (Exception ex)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        StatusLabel.Text = $"Request failed ({ex.Message})";
                    });
                }
            });
        }
    }
}
