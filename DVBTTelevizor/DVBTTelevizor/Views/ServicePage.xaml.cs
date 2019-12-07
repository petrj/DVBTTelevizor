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
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ServicePage : ContentPage
    {
        private ServicePageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected DVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;
        protected PlayerPage _playerPage;

        public ServicePage(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config, PlayerPage playerPage)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;
            _playerPage = playerPage;

            BindingContext = _viewModel = new ServicePageViewModel(_loggingService, _dialogService, _driver, _config);
            _viewModel.TuneFrequency = "730";
            _viewModel.SelectedDeliverySystemType = _viewModel.DeliverySystemTypes[0];

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateDriverState, (message) =>
            {
                _viewModel.UpdateDriverState();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    _viewModel.Status = $"Initialization failed ({message})";
                    _viewModel.UpdateDriverState();
                });
            });
        }

        private void ToolConnect_Clicked(object sender, EventArgs e)
        {
            if (!_viewModel.DriverConnected)
            {
                MessagingCenter.Send("", BaseViewModel.MSG_Init);
            }
            else
            {
                Task.Run(async () =>
                {
                    await _viewModel.DisconnectDriver();
                });
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();            
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }        

        public void Done()
        {
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_UpdateDriverState);
            MessagingCenter.Unsubscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed);
        }
    }
}