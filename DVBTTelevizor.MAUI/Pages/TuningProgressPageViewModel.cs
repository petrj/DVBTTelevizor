using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace DVBTTelevizor.MAUI
{
    public class TuningProgressPageViewModel : BaseViewModel
    {
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public TuningSettings Settings { get; set; }

        private int _actualTuningDVBTType = 0; // 0 .. DVBT, 1 .. DVBT2
        private long _actualTunningFreqKHz = 474000;

        private double _signalProgress = 0;
        private bool _signalSynced = false;
        private bool _signalLocked = false;
        private bool _signalCarrier = false;
        private long _signalSNR = 0;

        private double _signalStrengthProgress = 0;

        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();
        private Channel? _selectedChannel;

        private Dictionary<string, int> _tunedMultiplexes = new Dictionary<string, int>();
        private int _tunedNewChannels = 0;

        private TuneStateEnum _tuneState = TuneStateEnum.Inactive;
        private ListViewSelector? _listViewSelector = null;

        public event EventHandler? ChannelFound = null;

        public TuningProgressPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
            Settings = new TuningSettings(loggingService);

            ChannelFound += TuningProgressPageViewModel_ChannelFound;
            _driver.StatusChanged += TuningProgressPageViewModel_SignalChanged;

            _listViewSelector = new ListViewSelector(Channels);

            WeakReferenceMessenger.Default.Register<FontSizeChangedMessage>(this, (r, m) =>
            {
                _loggingService.Info($"TuningProgressPageViewModel: FontSizeChanged");
                NotifyFontSizeChange();
            });
        }

        private void TuningProgressPageViewModel_SignalChanged(object? sender, EventArgs e)
        {
            _loggingService.Info($"TuningProgressPageViewModel: TuningProgressPageViewModel_SignalChanged");

            if (e is DVBTDriverStatusChangedEventArgs se)
            {
                _signalProgress = se.Status.rfStrengthPercentage/100.0;
                _signalCarrier = se.Status.hasCarrier > 0;
                _signalLocked = se.Status.hasLock > 0;
                _signalSynced = se.Status.hasSync > 0;
                _signalSNR = se.Status.snr;

                NotifyChange();
            }
        }

        public void SelectFirstChannel()
        {
            _loggingService.Info($"Selecting first channel");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _selectedChannel = _listViewSelector?.SelectFirstChannel();
                NotifyChange();
            });
        }

        public void DeselectAll()
        {
            _loggingService.Info($"DeselectAll");

            _selectedChannel = null;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _listViewSelector?.DeselectAll();
                NotifyChange();
            });
        }

        public void SelectNextChannel()
        {
            _loggingService.Info($"Selecting next channel");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _selectedChannel = _listViewSelector?.SelectNextChannel();
                NotifyChange();
            });
        }

        public void SelectPreviousChannel()
        {
            _loggingService.Info($"Selecting previous channel");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                _selectedChannel = _listViewSelector?.SelectPreviousChannel();
                NotifyChange();
            });
        }

        public static string GetNextFreeChannelNumber(ObservableCollection<Channel> channels)
        {
            int num;
            int max = int.MinValue;
            var maxfound = false;
            foreach (var channel in channels)
            {
                if (int.TryParse(channel.Number, out num))
                {
                    if (num > max)
                    {
                        maxfound = true;
                        max = num;
                    }
                }
            }

            if (!maxfound)
            {
                return "1";
            }

            max++;

            return max.ToString();
        }

        private void TuningProgressPageViewModel_ChannelFound(object? sender, EventArgs e)
        {
            if (e is ChannelFoundEventArgs che)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var configChannels = _configuration.GetChannels();

                        // looking for the same channel
                        var channelAlreadyFound = false;
                        foreach (var configChannel in configChannels)
                        {
                            if (
                                (configChannel.ProgramMapPID == che.Channel.ProgramMapPID) &&
                                (configChannel.Frequency == che.Channel.Frequency)
                               )
                            {
                                channelAlreadyFound = true;
                                break;
                            }
                        }

                        Channels.Add(che.Channel);

                        if (che.Channel.ProviderName != null)
                        {
                            if (!_tunedMultiplexes.ContainsKey(che.Channel.ProviderName))
                            {
                                _tunedMultiplexes.Add(che.Channel.ProviderName, 0);
                            }
                            _tunedMultiplexes[che.Channel.ProviderName]++;
                        }

                        if (channelAlreadyFound)
                        {
                            _loggingService.Debug($"Found already tuned channel: \"{che.Channel.Name}\"");
                            return;
                        }

                        che.Channel.Number = GetNextFreeChannelNumber(configChannels);
                        _tunedNewChannels++;

                        configChannels.Add(che.Channel.Clone());

                        _configuration.SaveChannels(configChannels);

                        WeakReferenceMessenger.Default.Send(new ChannelsChangedMessage(String.Empty));

                    }
                    finally
                    {
                        NotifyChange();
                    }
                });
            }
        }

        public void ResetTune(bool clearChannels = true)
        {
            _loggingService.Info("RestartTune");

            if (clearChannels)
            {
                _loggingService.Info("Clearing channels");

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    _tunedMultiplexes.Clear();
                    _tunedNewChannels = 0;
                    Channels.Clear();
                });
            }

            _actualTuningDVBTType = 0;
            if (!DVBTTuning)
            {
                _actualTuningDVBTType = 1;
            }

            _actualTunningFreqKHz = FrequencyFromKHz;

            _signalSNR = 0;
            _signalCarrier = false;
            _signalLocked = false;
            _signalSynced = false;
            _signalProgress = 0;

            NotifyChange();
        }

        public async void StartTune()
        {
            if (State == TuneStateEnum.Inactive)
            {
                ResetTune();
            }

            if (State == TuneStateEnum.Finished)
            {
                ResetTune(false);
            }

            await Task.Run(async () => { await Tune(); });
        }

        private async Task Tune()
        {
            try
            {
                _loggingService.Info("Tuning started");

                _tuneState = TuneStateEnum.InProgress;

                //_savedChannels = await _channelService.LoadChannels();

                NotifyChange();

                for (var dvbtTypeIndex = 0; dvbtTypeIndex <= 1; dvbtTypeIndex++)
                {
                    if (FMTuning)
                    {
                        if (dvbtTypeIndex > 0)
                            continue;
                    } else
                    {
                        if (!DVBTTuning && dvbtTypeIndex == 0)
                            continue;
                        if (!DVBT2Tuning && dvbtTypeIndex == 1)
                            continue;
                        if (_actualTuningDVBTType > dvbtTypeIndex)
                        {
                            continue;
                        }
                    }
                    _actualTuningDVBTType = dvbtTypeIndex;

                    do
                    {
                        _loggingService.Info($"Tuning freq. {_actualTunningFreqKHz}");

                        await Tune(_actualTunningFreqKHz * 1000, TuneBandWidthKHz * 1000, dvbtTypeIndex);

                        if (FrequencyToKHz != FrequencyFromKHz)
                        {
                            _actualTunningFreqKHz += TuneBandWidthKHz;
                        } else
                        {
                            break;
                        }

                        if (State != TuneStateEnum.InProgress)
                        {
                            return;
                        }

                        NotifyChange();

                    } while (_actualTunningFreqKHz <= FrequencyToKHz);

                    if (dvbtTypeIndex == 0 && DVBT2Tuning)
                    {
                        // reset position to DVBT2
                        _actualTunningFreqKHz = FrequencyFromKHz;
                    }
                }

                State = TuneStateEnum.Finished;
                //SignalStrengthProgress = 0;
                //MessagingCenter.Send("FinishButton", BaseViewModel.MSG_UpdateTuningPageFocus);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                State = TuneStateEnum.Failed;
            }
            finally
            {
                _loggingService.Info("Tuning finished");
                NotifyChange();
            }
        }

        private async Task Tune(long freq, long bandWidth, int dvbtTypeIndex)
        {
            try
            {
                //SignalStrengthProgress = 0;

                var tuneResult = await _driver.TuneEnhanced(freq, bandWidth, dvbtTypeIndex, false);

                switch (tuneResult.Result)
                {
                    case DVBTDriverSearchProgramResultEnum.Error:
                        _loggingService.Debug("Search error");
                        return;

                    case DVBTDriverSearchProgramResultEnum.NoSignal:
                        _loggingService.Debug("No signal");
                        return;
                }

                var searchMapPIDsResult = await _driver.SearchProgramMapPIDs(false);

                switch (searchMapPIDsResult.Result)
                {
                    case DVBTDriverSearchProgramResultEnum.Error:
                        _loggingService.Debug("Search error");

                        return;

                    case DVBTDriverSearchProgramResultEnum.NoSignal:
                        _loggingService.Debug("No signal");

                        return;

                    case DVBTDriverSearchProgramResultEnum.NoProgramFound:
                        _loggingService.Debug("No program found");

                        return;
                }

                if (State != TuneStateEnum.InProgress)
                {
                    _loggingService.Debug($"Tuning aborted");
                    return;
                }

                var totalChannelsAddedCount = 0;

                var mapPIDToServiceDescriptor = new Dictionary<long, MPEGTS.ServiceDescriptor>();

                var configChannels = _configuration.GetChannels();

                foreach (var serviceDescriptor in searchMapPIDsResult.ServiceDescriptors)
                {
                    // ProgramMapPID must be unique!
                    if (!(mapPIDToServiceDescriptor.ContainsKey(serviceDescriptor.Value)))
                    {
                        mapPIDToServiceDescriptor.Add(serviceDescriptor.Value, null);
                    }
                    else
                    {
                        _loggingService.Debug($"Not unique MapPID {serviceDescriptor.Value}!");
                        continue;
                    }

                    var ch = new Channel();
                    ch.ProgramMapPID = serviceDescriptor.Value;
                    ch.Name = serviceDescriptor.Key.ServiceName;
                    ch.ProviderName = serviceDescriptor.Key.ProviderName;
                    ch.Frequency = freq;
                    ch.Bandwdith = bandWidth;
                    ch.Number = String.Empty;
                    ch.DVBTType = dvbtTypeIndex;
                    ch.Type = (ServiceTypeEnum)serviceDescriptor.Key.ServisType;
                    //ch.NonFree = !serviceDescriptor.Key.Free;

                    /*
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        TunedChannels.Add(ch);
                        OnPropertyChanged(nameof(TunedChannelsCount));
                        OnPropertyChanged(nameof(NewTunedChannelsCount));
                        OnPropertyChanged(nameof(TunedMultiplexesCount));
                    });
                    */
                    _loggingService.Debug($"Found channel \"{serviceDescriptor.Key.ServiceName}\"");

                    if (ChannelFound != null)
                    {
                        ChannelFound(this, new ChannelFoundEventArgs() { Channel = ch });
                    }


                    /*
                    // automatically adding new tuned channel if does not exist
                    if (!ConfigViewModel.ChannelExists(_savedChannels, ch.FrequencyAndMapPID))
                    {
                        ch.Number = ConfigViewModel.GetNextChannelNumber(_savedChannels).ToString();

                        _savedChannels.Add(ch);

                        await _channelService.SaveChannels(_savedChannels);
                        totalChannelsAddedCount++;
                        _newTunedChannelsCount++;
                    }
                    */
                }

                /*
                if (totalChannelsAddedCount > 0)
                {
                    if (totalChannelsAddedCount > 1)
                    {
                        MessagingCenter.Send($"{totalChannelsAddedCount} channels saved", BaseViewModel.MSG_ToastMessage);
                    }
                    else
                    {
                        MessagingCenter.Send($"Channel saved", BaseViewModel.MSG_ToastMessage);
                    }
                }
                */
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                throw;
            }
        }

        public void StopTune()
        {
            _tuneState = TuneStateEnum.Stopped;

            NotifyChange();
        }

        public enum TuneStateEnum
        {
            Inactive = 0,
            InProgress = 1,
            Stopped = 2,
            Finished = 3,
            Failed = 4
        }

        public long TuneBandWidthKHz
        {
            get
            {
                return Settings.BandwidthKHz;
            }
        }

        public void UpdateActualFreq()
        {
            if (_actualTunningFreqKHz > Settings.FrequencyToKHz || _actualTunningFreqKHz < Settings.FrequencyFromKHz)
            {
                _actualTunningFreqKHz = Settings.FrequencyFromKHz;
            }

            if (!DVBTTuning && _actualTuningDVBTType == 0)
            {
                _actualTuningDVBTType = 1;
                _actualTunningFreqKHz = Settings.FrequencyFromKHz;
            }

            NotifyChange();
        }

        public Channel? SelectedChannel
        {
            get
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    return _selectedChannel;
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
            set
            {
                _semaphoreSlim.WaitAsync();
                try
                {
                    _selectedChannel = value;

                    NotifyChange();
                }
                finally
                {
                    _semaphoreSlim.Release();
                };
            }
        }

        public void NotifyChange()
        {
            //_loggingService.Debug("NotifyChange");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(FrequencyKHz));
                OnPropertyChanged(nameof(FrequencyWholePartMHz));
                OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));

                OnPropertyChanged(nameof(DeliverySystem));
                OnPropertyChanged(nameof(SubTitleCaption));

                OnPropertyChanged(nameof(TuningProgress));
                OnPropertyChanged(nameof(FrequencyProgress));
                OnPropertyChanged(nameof(TuningInProgress));
                OnPropertyChanged(nameof(TuningProgressVisible));
                OnPropertyChanged(nameof(TuningProgressCaption));
                OnPropertyChanged(nameof(State));

                OnPropertyChanged(nameof(TuneBandWidthKHz));
                OnPropertyChanged(nameof(DVBTTuning));
                OnPropertyChanged(nameof(DVBT2Tuning));
                OnPropertyChanged(nameof(FMTuning));

                OnPropertyChanged(nameof(FrequencyFromKHz));
                OnPropertyChanged(nameof(FrequencyToKHz));
                OnPropertyChanged(nameof(FrequencyFromMHz));
                OnPropertyChanged(nameof(FrequencyFromMHzTitle));
                OnPropertyChanged(nameof(FrequencyToMHz));
                OnPropertyChanged(nameof(FrequencyToMHzTitle));

                OnPropertyChanged(nameof(SignalProgressCaption));
                OnPropertyChanged(nameof(SignalProgress));

                OnPropertyChanged(nameof(SignalCarrier));
                OnPropertyChanged(nameof(SignalLocked));
                OnPropertyChanged(nameof(SignalSynced));
                OnPropertyChanged(nameof(SignalSNR));
                OnPropertyChanged(nameof(Bitrate));

                OnPropertyChanged(nameof(Channels));
                OnPropertyChanged(nameof(SelectedChannel));

                OnPropertyChanged(nameof(StartButtonVisible));
                OnPropertyChanged(nameof(ContinueButtonVisible));
                OnPropertyChanged(nameof(StopButtonVisible));
                OnPropertyChanged(nameof(BackButtonVisible));
                OnPropertyChanged(nameof(FinishButtonVisible));

                OnPropertyChanged(nameof(TunedMultiplexesCount));
                OnPropertyChanged(nameof(TunedChannelsCount));
                OnPropertyChanged(nameof(TunedNewChannelsCount));

                OnPropertyChanged(nameof(SignalStrengthProgress));
            });
        }

        public TuneStateEnum State
        {
            get
            {
                return _tuneState;
            }
            set
            {
                _tuneState = value;
                NotifyChange();
            }
        }

        public int TunedMultiplexesCount
        {
            get
            {
                if (_tunedMultiplexes == null)
                {
                    return 0;
                }

                return _tunedMultiplexes.Count;
            }
        }

        public int TunedChannelsCount
        {
            get
            {
                if (Channels == null)
                {
                    return 0;
                }

                return Channels.Count;
            }
        }

        public int TunedNewChannelsCount
        {
            get
            {
                return _tunedNewChannels;
            }
        }

        public int DeliverySystem
        {
            get
            {
                return _actualTuningDVBTType;
            }
            set
            {
                _actualTuningDVBTType = value;
                NotifyChange();
            }
        }

        public string SubTitleCaption
        {
            get
            {
                var res = "";
                res += DeliverySystem == 0 ? "     DVBT" : "     DVBT2";
                /*
                if (FrequencyFromKHz != FrequencyToKHz)
                {
                    res += $" ({FrequencyFromMHz}-{FrequencyToMHz})";
                }
                res += " MHz";
                */

                return res;
            }
        }

        public bool TuningInProgress
        {
            get
            {
                return State == TuneStateEnum.InProgress;
            }
        }

        public bool TuningProgressVisible
        {
            get
            {
                return TuningInProgress;
            }
        }

        public long FrequencyWholePartMHz
        {
            get
            {
                return Convert.ToInt64(Math.Floor(FrequencyKHz / 1000.0));
            }
        }

        public string FrequencyDecimalPartMHzCaption
        {
            get
            {
                var part = (FrequencyKHz / 1000.0) - FrequencyWholePartMHz;
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000} MHz";
            }
        }

        public long FrequencyKHz
        {
            get
            {
                return _actualTunningFreqKHz;
            }
            set
            {
                _actualTunningFreqKHz = value;

                NotifyChange();
            }
        }

        public bool DVBTTuning
        {
            get
            {
                return Settings.DVBT;
            }
        }

        public bool FMTuning
        {
            get
            {
                return Settings.FM;
            }
        }

        public bool SignalCarrier
        {
            get
            {
                return _signalCarrier;
            }
            set
            {
                _signalCarrier = value;

                NotifyChange();
            }
        }

        public bool SignalLocked
        {
            get
            {
                return _signalLocked;
            }
            set
            {
                _signalLocked = value;

                NotifyChange();
            }
        }

        public bool SignalSynced
        {
            get
            {
                return _signalSynced;
            }
            set
            {
                _signalSynced = value;

                NotifyChange();
            }
        }

        public long SignalSNR
        {
            get
            {
                return _signalSNR;
            }
            set
            {
                _signalSNR = value;

                NotifyChange();
            }
        }

        public string Bitrate
        {
            get
            {
                if (_driver == null)
                    return "0";

                return DVBTDriverConnector.GetHumanReadableBitRate(_driver.Bitrate);
            }
        }

        public bool DVBT2Tuning
        {
            get
            {
                return Settings.DVBT2;
            }
        }

        public long FrequencyFromKHz
        {
            get
            {
                return Settings.FrequencyFromKHz;
            }
        }

        public long FrequencyToKHz
        {
            get
            {
                return Settings.FrequencyToKHz;
            }
        }

        public long FrequencyFromMHz
        {
            get
            {
                return Settings.FrequencyFromKHz / 1000;
            }
        }

        public string FrequencyFromMHzTitle
        {
            get
            {
                return "< " + FrequencyFromMHz.ToString();
            }
        }

        public string FrequencyToMHzTitle
        {
            get
            {
                return FrequencyToMHz.ToString() + " >";
            }
        }

        public long FrequencyToMHz
        {
            get
            {
                return Settings.FrequencyToKHz / 1000;
            }
        }

        public double FrequencyProgress
        {
            get
            {
                if (_actualTunningFreqKHz < FrequencyFromKHz)
                    return 0.0;

                if (_actualTunningFreqKHz > FrequencyToKHz)
                    return 100.0;

                if (FrequencyFromKHz == FrequencyToKHz)
                {
                    if (State == TuneStateEnum.Finished)
                    {
                        return 100.0;
                    }

                    return 0.0;
                }

                var onePerc = (FrequencyToKHz - FrequencyFromKHz) / 100.0;
                if (onePerc == 0)
                    return 0.0;

                var perc = (_actualTunningFreqKHz - FrequencyFromKHz) / onePerc;

                if (perc < 0)
                    return 0.0;

                if (perc > 100)
                    return 100.0;

                return perc / 100.0;
            }
        }

        public double TuningProgress
        {
            get
            {
                if (DVBTTuning && DVBT2Tuning && !FMTuning)
                {
                    var perc = FrequencyProgress / 2.0;
                    if (_actualTuningDVBTType == 1)
                    {
                        perc += 0.5;
                    }
                    return perc;
                }
                else
                {
                    return FrequencyProgress;
                }
            }
        }

        public string TuningProgressCaption
        {
            get
            {
                var tpr = Convert.ToInt32(TuningProgress * 100.0);
                return (tpr > 100 ? 100 : tpr).ToString() + " %";
            }
        }

        public double SignalProgress
        {
            get
            {
                return _signalProgress;
            }
            set
            {
                _signalProgress = value;

                NotifyChange();
            }
        }

        public string SignalProgressCaption
        {
            get
            {
                return (_signalProgress*100.0).ToString("N0") + " %";
            }
        }

        public bool ContinueButtonVisible
        {
            get
            {
                return State == TuneStateEnum.Stopped;
            }
        }

        public bool StartButtonVisible
        {
            get
            {
                return State != TuneStateEnum.InProgress;
            }
        }

        public bool StopButtonVisible
        {
            get
            {
                return State == TuneStateEnum.InProgress;
            }
        }

        public bool BackButtonVisible
        {
            get
            {
                return State != TuneStateEnum.InProgress;
            }
        }

        public bool FinishButtonVisible
        {
            get
            {
                return
                    (State == TuneStateEnum.Finished)
                    ||
                    (State == TuneStateEnum.Failed)
                    ||
                    (State == TuneStateEnum.Stopped);
            }
        }

        public double SignalStrengthProgress
        {
            get
            {
                return _signalStrengthProgress;
            }
            set
            {
                _signalStrengthProgress = value;

                NotifyChange();
            }
        }

        public async void SelectChannelsListView(ListView list)
        {
            _loggingService.Info($"Selecting ChannelsListView");

            await Task.Run(
                () =>
                {
                    if (Channels.Count == 0)
                    {
                        SelectedChannel = null;
                        return;
                    }

                    // selecting first channel
                    foreach (var ch in Channels)
                    {
                        SelectedChannel = ch;
                        return;
                    }

                    list.Focus();
                });
        }
    }
}

