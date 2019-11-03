using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LoggerService;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Threading;
using Newtonsoft.Json;

namespace DVBTTelevizor
{
    public class MainPageViewModel : BaseViewModel
    {
        public Command RefreshCommand { get; set; }


        private DVBTChannel _selectedChannel;

        public ObservableCollection<DVBTChannel> Channels { get; set; } = new ObservableCollection<DVBTChannel>();

        public MainPageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
            :base(loggingService, dialogService, driver, config)
        {
           RefreshCommand = new Command(async () => await Refresh());

           RefreshCommand.Execute(null);
        }

        public DVBTChannel SelectedChannel
        {
            get
            {
                return _selectedChannel;
            }
            set
            {
                _loggingService.Debug($"Selected channel {value}");

                _selectedChannel = value;                

                OnPropertyChanged(nameof(SelectedChannel));

               Task.Run(async () =>
               {
                   await PlayChannel(_selectedChannel);
               });
            }
        }

        public async Task SaveChannelsToConfig()
        {
            await Task.Run(() =>
            {
                _loggingService.Debug($"Saving channels to config");

                _config.Channels = Channels;
            });
        }

        private async Task PlayChannel(DVBTChannel channel)
        {
            _loggingService.Debug($"Playing channel {channel}");

            var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary);
            if (!playRes)
                return;

            MessagingCenter.Send(channel.Name, "PlayStream");
        }

        private async Task Refresh()
        {
            try
            {
                _loggingService.Info($"Refreshing channels");

               Channels.Clear();

                var chService = new JSONChannelsService(_loggingService, _config);

                ObservableCollection<DVBTChannel> channels = null;

                await RunWithStoragePermission(
                    async () =>
                    {
                        channels = await chService.LoadChannels();
                    }, _dialogService);

                // adding one by one
                foreach (var ch in channels)
                {
                    Channels.Add(ch);
                }

                OnPropertyChanged(nameof(Channels));

            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while loading channels");
            }
        }


    }
}
