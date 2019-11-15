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
        ChannelService _channelService;

        public Command RefreshCommand { get; set; }

        public Command LongPressCommand { get; set; }
        public Command ShortPressCommand { get; set; }

        private DVBTChannel _selectedChannel;
        private DVBTChannel _recordingChannel;

        public bool DoNotScrollToChannel { get; set; } = false;

        public ObservableCollection<DVBTChannel> Channels { get; set; } = new ObservableCollection<DVBTChannel>();

        public MainPageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
            :base(loggingService, dialogService, driver, config)
        {
            _channelService = channelService;

            RefreshCommand = new Command(async () => await Refresh());
            LongPressCommand = new Command(async (itm) => await LongPress(itm));
            ShortPressCommand = new Command(ShortPress);

            RefreshCommand.Execute(null);

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfiguration, (message) =>
            {
                _loggingService.Debug($"Received DVBTDriverConfiguration message: {message}");

                if (!_driver.Started)
                {
                    ConnectDriver(message);
                }
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateDriverState, (message) =>
            {
                UpdateDriverState();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    Status = $"Initialization failed ({message})";
                    UpdateDriverState();
                });
            });
        }

        private async Task LongPress(object item)
        {
            if (item != null && item is DVBTChannel)
            {
                var ch = item as DVBTChannel;

                SelectedChannel = ch;

                _loggingService.Info($"Long press on channel {ch.Name})");

                var actions = new List<string>();
                actions.Add("Rename");

                if (ch.Recording)
                {
                    actions.Add("Stop record");
                } else
                {
                    actions.Add("Record");
                }

                actions.Add("Delete");

                var action = await _dialogService.DisplayActionSheet($"{ch.Name}", "Cancel", actions);

                switch (action)
                {
                    case "Record":
                        _recordingChannel = ch;
                        break;
                    case "Stop record":
                        _recordingChannel = null;
                        break;
                }

                await Refresh();
            }
        }

        private void ShortPress(object item)
        {
            if (item != null && item is DVBTChannel)
            {
                // select and play

                DoNotScrollToChannel = true;

                var ch = item as DVBTChannel;

                SelectedChannel = ch;

                _loggingService.Info($"Short press on channel {ch.Name})");

                Task.Run(async () => await PlayChannel(ch));
            }
        }

        public async Task SelectChannelByNumber(string number)
        {
            _loggingService.Info($"Selecting channel by number {number}");

            await Task.Run(
                () =>
                {
                    // looking for channel by its number:
                    foreach (var ch in Channels)
                    {
                        if (ch.Number.ToString() == number)
                        {
                            SelectedChannel = ch;
                            break;
                        }
                    }
                });
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

        public async Task PlayChannel(DVBTChannel channel = null)
        {
            if (channel == null)
            {
                channel = SelectedChannel;
                if (channel == null)
                    return;
            }

            _loggingService.Debug($"Playing channel {channel}");

            var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary);
            if (!playRes)
                return;

            MessagingCenter.Send(channel.Name, BaseViewModel.MSG_PlayStream);
        }

        private async Task Refresh()
        {
            try
            {
                _loggingService.Info($"Refreshing channels");

                var selectedChannel = _selectedChannel;
                DVBTChannel selectChannel = null;

                Channels.Clear();

                ObservableCollection<DVBTChannel> channels = null;

                await RunWithStoragePermission(
                    async () =>
                    {
                        channels = await _channelService.LoadChannels();
                    }, _dialogService);

                // channels filter

                foreach (var ch in channels)
                {
                    if (ch.ServiceType == DVBTServiceType.TV && !_config.ShowTVChannels)
                        continue;

                    if (ch.ServiceType == DVBTServiceType.Radio && !_config.ShowRadioChannels)
                        continue;

                    if (_recordingChannel != null &&
                        _recordingChannel.Frequency == ch.Frequency &&
                        _recordingChannel.Name == ch.Name &&
                        _recordingChannel.ProgramMapPID == ch.ProgramMapPID)
                    {
                        ch.Recording = true;
                    }

                    if (selectedChannel != null &&
                        selectedChannel.Frequency == ch.Frequency &&
                        selectedChannel.Name == ch.Name &&
                        selectedChannel.ProgramMapPID == ch.ProgramMapPID)
                    {
                        selectChannel = ch;
                    }

                    Channels.Add(ch);
                }

                OnPropertyChanged(nameof(Channels));

                if (selectChannel != null)
                    SelectedChannel = selectChannel;

            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while loading channels");
            }
        }

        public async Task SelectNextChannel(int step = 1)
        {
            _loggingService.Info($"Selecting next channel (step {step})");

            await Task.Run(
                () =>
                {

                    if (Channels.Count == 0)
                        return;

                    if (SelectedChannel == null)
                    {
                        SelectedChannel = Channels[0];
                    }
                    else
                    {
                        bool next = false;
                        var nextCount = 1;
                        for (var i = 0; i < Channels.Count; i++)
                        {
                            var ch = Channels[i];

                            if (next)
                            {
                                if (nextCount == step || i == Channels.Count - 1)
                                {
                                    SelectedChannel = ch;
                                    break;
                                }
                                else
                                {
                                    nextCount++;
                                }
                            }
                            else
                            {
                                if (ch == SelectedChannel)
                                {
                                    next = true;
                                }
                            }
                        }
                    }
                });
        }

        public async Task SelectPreviousChannel(int step = 1)
        {
            _loggingService.Info($"Selecting previous channel (step {step})");

            await Task.Run(
                () =>
                {

                        if (Channels.Count == 0)
                            return;

                        if (SelectedChannel == null)
                        {
                            SelectedChannel = Channels[Channels.Count - 1];
                        }
                        else
                        {
                            bool previous = false;
                            var previousCount = 1;

                            for (var i = Channels.Count - 1; i >= 0; i--)
                            {
                                if (previous)
                                {
                                    if (previousCount == step || i == 0)
                                    {
                                        SelectedChannel = Channels[i];
                                        break;
                                    }
                                    else
                                    {
                                        previousCount++;
                                    }
                                }
                                else
                                {
                                    if (Channels[i] == SelectedChannel)
                                    {
                                        previous = true;
                                    }
                                }
                            }
                        }
                });
        }

    }
}
