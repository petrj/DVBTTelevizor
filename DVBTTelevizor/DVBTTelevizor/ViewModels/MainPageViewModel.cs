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

        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public Command RefreshCommand { get; set; }
        public Command RefreshEPGCommand { get; set; }

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
            RefreshEPGCommand = new Command(async () => await RefreshEPG());
            LongPressCommand = new Command(async (itm) => await LongPress(itm));
            ShortPressCommand = new Command(ShortPress);

            BackgroundCommandWorker.RunInBackground(RefreshEPGCommand, 5, 5);
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
                if (!ch.Recording)
                {
                    actions.Add("Record");
                }
                else
                {
                    actions.Add("Stop record");
                }
                actions.Add("Edit");
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
                    if (!_driver.Started)
                    {
                        MessagingCenter.Send($"Recording failed (tuner connection error)", BaseViewModel.MSG_ToastMessage);
                        return;
                    }

                    var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary, false);
                    if (!playRes)
                    {
                        throw new Exception("Play returned false");
                    }

                    _recordingChannel = channel;
                    await _driver.StartRecording();
                } else
                {
                    if (!_driver.Started)
                    {
                        MessagingCenter.Send($"Stop recording failed (tuner connection error)", BaseViewModel.MSG_ToastMessage);
                        return;
                    }

                    _driver.StopRecording();
                    await _driver.Stop();
                    _recordingChannel = null;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);

                MessagingCenter.Send($"Start/stop recording failed (tuner connection error)", BaseViewModel.MSG_ToastMessage);

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

        public async Task SelectChannelByFrequencyAndMapPID(string frequencyAndMapPID)
        {
            _loggingService.Info($"Selecting channel by frequency and mapPID {frequencyAndMapPID}");

            await Task.Run(
                () =>
                {
                    DVBTChannel firstChannel = null;
                    DVBTChannel selectChannel = null;

                    if (Channels.Count == 0)
                        return;

                    foreach (var ch in Channels)
                    {
                        if (firstChannel == null)
                        {
                            firstChannel = ch;
                        }

                        if (ch.Frequency.ToString() + ch.ProgramMapPID.ToString() == frequencyAndMapPID)
                        {
                            selectChannel = ch;
                            break;
                        }
                    }

                    if (selectChannel == null)
                        selectChannel = firstChannel;

                    SelectedChannel = selectChannel;
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

            var playInfo = new PlayStreamInfo
            {
                Channel = channel
            };

            if (_recordingChannel != null)
            {
                MessagingCenter.Send($"Playing {channel.Name} failed (recording in progress)", BaseViewModel.MSG_ToastMessage);
                return;
            }

            _loggingService.Debug($"Playing channel {channel}");

            try
            {
                if (!_driver.Started)
                {
                    MessagingCenter.Send($"Playing {channel.Name} failed (tuner connection error)", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                IsBusy = true;

                var playRes = await _driver.Play(channel.Frequency, channel.Bandwdith, channel.DVBTType, channel.PIDsArary);
                if (!playRes)
                {
                    throw new Exception("Play returned false");
                }

                var eitManager = _driver.GetEITManager(channel.Frequency);
                if (eitManager != null)
                {
                    playInfo.CurrentEvent = eitManager.GetEvent(DateTime.Now, Convert.ToInt32(channel.ProgramMapPID));
                }

                MessagingCenter.Send(playInfo, BaseViewModel.MSG_PlayStream);

            } catch (Exception ex)
            {
                _loggingService.Error(ex);

                MessagingCenter.Send($"Playing {channel.Name} failed", BaseViewModel.MSG_ToastMessage);
            } finally
            {
                IsBusy = false;
            }

        }

        private async Task Refresh()
        {
            string selectedChanneFrequencyAndMapPID= null;

            try
            {
                IsBusy = true;

                await _semaphoreSlim.WaitAsync();

                DoNotScrollToChannel = true;

                _loggingService.Info($"Refreshing channels");

                if (SelectedChannel != null)
                {
                    selectedChanneFrequencyAndMapPID = SelectedChannel.Frequency.ToString() + SelectedChannel.ProgramMapPID.ToString();
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

                    if (ch.SimplifiedServiceType == DVBTServiceType.Other && !_config.ShowOtherChannels)
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

                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(TunningButtonVisible));

                await SelectChannelByFrequencyAndMapPID(selectedChanneFrequencyAndMapPID);

                DoNotScrollToChannel = false;
            }
        }

        private async Task RefreshEPG()
        {
            if (!_config.ScanEPG)
                return;

            _loggingService.Info($"RefreshEPG");

            try
            {  
                await _semaphoreSlim.WaitAsync();

                IsBusy = true;

                foreach (var channel in Channels)
                {
                    channel.ClearEPG();

                    var eitM = _driver.GetEITManager(channel.Frequency);

                    if (eitM != null)
                    {
                        var evs = eitM.GetEvents(DateTime.Now, 2);
                        var programMapPID = Convert.ToInt32(channel.ProgramMapPID);
                        if (evs != null && evs.ContainsKey(programMapPID))
                        {
                            if (evs[programMapPID] != null)
                            {
                                if (evs[programMapPID].Count > 0)
                                    channel.CurrentEventItem = evs[programMapPID][0];

                                if (evs[programMapPID].Count > 1)
                                    channel.NextEventItem = evs[programMapPID][1];

                                channel.NotifyEPGChanges();
                            }
                        }
                    }
                }
            }
            finally
            {
                IsBusy = false;

                _semaphoreSlim.Release();

                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(SelectedChannel));
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
