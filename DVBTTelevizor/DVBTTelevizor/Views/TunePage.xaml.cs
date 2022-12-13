﻿using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace DVBTTelevizor
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TunePage : ContentPage, IOnKeyDown
    {
        private TunePageViewModel _viewModel;
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected IDVBTDriverManager _driver;
        protected DVBTTelevizorConfiguration _config;

        public TunePage(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            InitializeComponent();

            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;
            _config = config;

            BindingContext = _viewModel = new TunePageViewModel(_loggingService, _dialogService, _driver, _config, channelService);
            _viewModel.TuneFrequency = "730";

            ChannelsListView.ItemSelected += ChannelsListView_ItemSelected;

            Appearing += TunePage_Appearing;

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateDriverState, (message) =>
            {
                _viewModel.UpdateDriverState();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    _viewModel.UpdateDriverState();
                });
            });
        }

        private void TunePage_Appearing(object sender, EventArgs e)
        {
            _viewModel.NotifyFontSizeChange();
            TuneButton.Focus();
        }

        private async void ToolConnect_Clicked(object sender, EventArgs e)
        {
            if (_driver.Started)
            {
                if (!(await _dialogService.Confirm($"Connected device: {_driver.Configuration.DeviceName}.", $"Device status", "Back", "Disconnect")))
                {
                    await _viewModel.DisconnectDriver();
                }
            }
            else
            {

                if (await _dialogService.Confirm($"Disconnected.", $"Device status", "Connect", "Back"))
                {
                    MessagingCenter.Send("", BaseViewModel.MSG_Init);
                }
            }
        }
        private void ChannelsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            ChannelsListView.ScrollTo(_viewModel.SelectedChannel, ScrollToPosition.MakeVisible, false);
        }

        public async void OnKeyDown(string key, bool longPress)
        {
            _loggingService.Debug($"TunePage OnKeyDown {key}");
        }
    }
}