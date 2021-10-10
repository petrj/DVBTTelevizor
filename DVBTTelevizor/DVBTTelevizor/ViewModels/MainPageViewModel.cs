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
using System.Linq;

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
        }

        public bool ShowServiceMenuToolItem
        {
            get
            {
                return _config.ShowServiceMenu;
            }
        }

        private async Task LongPress(object item)
        {
            if (item != null && item is DVBTChannel)
            {
                var ch = item as DVBTChannel;

                SelectedChannel = ch;

                _loggingService.Info($"Long press on channel {ch.Name})");

                var actions = new List<string>();
                actions.Add("Play");
                actions.Add("Edit");

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
                    case "Play":
                        await PlayChannel(ch);
                        break;
                    case "Edit":
                        MessagingCenter.Send(ch.ToString(), BaseViewModel.MSG_EditChannel);
                        break;
                    case "Record":
                        await RecordChannel(ch, true);
                        break;
                    case "Stop record":
                        await RecordChannel(ch, false);
                        break;
                    case "Delete":
                        await DeleteChannel(ch);
                        break;
                }
            }
        }

        private async Task RecordChannel(DVBTChannel channel, bool start)
        {
            if (channel == null)
            {
                channel = SelectedChannel;
                if (channel == null)
                    return;
            }

            _loggingService.Debug($"Recording channel {channel}");

            try
            {
                if (start)
                {
                    var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary, false);
                    if (!playRes)
                    {
                        throw new Exception("Play returned false");
                    }

                    _recordingChannel = channel;
                    await _driver.StartRecording();
                } else
                {
                    _driver.StopRecording();
                    await _driver.Stop();
                    _recordingChannel = null;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);

                Device.BeginInvokeOnMainThread(async () =>
                           await _dialogService.Error($"Start/stop rec error")
                        );
                return;
            }

            await Refresh();
        }

        private async Task DeleteChannel(DVBTChannel channel)
        {
            if (await _dialogService.Confirm($"Are you sure to delete channel {channel.Name}?"))
            {
                Channels.Remove(channel);
                await _channelService.SaveChannels(Channels);
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
                        if (ch.Number == number)
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

        public bool TunningButtonVisible
        {
            get
            {
                return Channels.Count == 0;
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

            if (_recordingChannel != null)
            {
                MessagingCenter.Send($"Playing {channel.Name} failed, recording in progress", BaseViewModel.MSG_ToastMessage);
            }

            _loggingService.Debug($"Playing channel {channel}");

            try
            {
                var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary);
                if (!playRes)
                {
                    throw new Exception("Play returned false");
                }

                var playInfo = new PlayStreamInfo
                {
                    Channel = channel
                };

                if (_driver.EITManager != null)
                {
                    var events = _driver.EITManager.GetEvents(DateTime.Now, 1);
                    var mapPID = channel.ProgramMapPID;
                    if (events.ContainsKey((int)mapPID))
                    {
                        var evs = events[(int)mapPID];
                        if (evs != null && evs.Count > 0)
                        {
                            playInfo.CurrentEvent = evs[0];
                        }
                    }
                }

                MessagingCenter.Send(playInfo, BaseViewModel.MSG_PlayStream);

            } catch (Exception ex)
            {
                _loggingService.Error(ex);

                MessagingCenter.Send($"Playing {channel.Name} failed", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task Refresh()
        {
            string selectedChannelNumber = null;

            try
            {
                IsBusy = true;

                _loggingService.Info($"Refreshing channels");                

                if (SelectedChannel == null)
                {
                    selectedChannelNumber = "1";
                } else
                {
                    selectedChannelNumber = SelectedChannel.Number;
                }

                Channels.Clear();

                ObservableCollection<DVBTChannel> channels = null;

                channels = await _channelService.LoadChannels();

                // sort chanels by number

                channels = new ObservableCollection<DVBTChannel>(channels.OrderBy(i => i.Number.PadLeft(4,'0')));

                // channels filter

                foreach (var ch in channels)
                {
                    if (ch.SimplifiedServiceType == DVBTServiceType.TV && !_config.ShowTVChannels)
                        continue;

                    if (ch.SimplifiedServiceType == DVBTServiceType.Radio && !_config.ShowRadioChannels)
                        continue;

                    if (_recordingChannel != null &&
                        _recordingChannel.Frequency == ch.Frequency &&
                        _recordingChannel.Name == ch.Name &&
                        _recordingChannel.ProgramMapPID == ch.ProgramMapPID)
                    {
                        ch.Recording = true;
                    }

                    Channels.Add(ch);
                }
              

            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while loading channels");
            }
            finally
            {
                IsBusy = false;

                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(TunningButtonVisible));

                await SelectChannelByNumber(selectedChannelNumber);
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
