using LoggerService;
using Microsoft.Maui;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class TuningProgressPageViewModel : BaseViewModel
    {
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        private int _actualTuningDVBTType = 0; // 0 .. DVBT, 1 .. DVBT2
        private long _actualTunningFreqKHz = 474000;
        private long _tuneBandWidthKHz = 8000;

        private long _frequencyFromKHz = 474000;
        private long _frequencyToKHz = 852000;

        private double _signalProgress = 0;

        private bool _DVBTTuning = true;
        private bool _DVBT2Tuning = true;

        public ObservableCollection<Channel> Channels { get; set; } = new ObservableCollection<Channel>();
        private Channel? _selectedChannel;

        private Dictionary<long,string> _tunedMultiplexes = new Dictionary<long,string>();
        private int _tunedNewChannels = 0;

        private TuneStateEnum _tuneState = TuneStateEnum.Inactive;

        public TuningProgressPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {

        }

        private void RestartTune()
        {
            _tunedMultiplexes.Clear();
            _tunedNewChannels = 0;
            Channels.Clear();

            Channels.Add(new Channel()
            {
                Number = "1",
                Name = "CT1",
                ProviderName = "Cesta televize",
                Bandwdith = 8,
                DVBTType = 1,
                Frequency = 484000000,
                Type = MPEGTS.ServiceTypeEnum.DigitalTelevisionService,
                NonFree = true
            });
            Channels.Add(new Channel()
            {
                Number = "2",
                Name = "CT2",
                ProviderName = "Cesta televize",
                Bandwdith = 8,
                DVBTType = 1,
                Frequency = 484000000,
                Type = MPEGTS.ServiceTypeEnum.DigitalTelevisionService,
                NonFree = false
            });

            _actualTuningDVBTType = 0;
            if (!DVBTTuning)
            {
                _actualTuningDVBTType = 1;
            }

            _actualTunningFreqKHz = FrequencyFromKHz;
        }

        public async void StartTune()
        {
            if (State == TuneStateEnum.Inactive)
            {
                RestartTune();
            }

            await Task.Run( async () => { await Tune(); });
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
                    if (!DVBTTuning && dvbtTypeIndex == 0)
                        continue;
                    if (!DVBT2Tuning && dvbtTypeIndex == 1)
                        continue;
                    if (_actualTuningDVBTType>dvbtTypeIndex)
                    {
                        continue;
                    }
                    _actualTuningDVBTType = dvbtTypeIndex;

                    do
                    {
                        _loggingService.Info($"Tuning freq. {_actualTunningFreqKHz}");

                        await Tune(_actualTunningFreqKHz * 1000, TuneBandWidthKHz * 1000, dvbtTypeIndex);

                        if (FrequencyToKHz != FrequencyFromKHz)
                        {
                            _actualTunningFreqKHz += TuneBandWidthKHz;
                        }

                        if (State != TuneStateEnum.InProgress)
                        {
                            return;
                        }

                        NotifyChange();

                    } while (_actualTunningFreqKHz <= FrequencyToKHz);
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
                    ch.NonFree = !serviceDescriptor.Key.Free;

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
                return _tuneBandWidthKHz;
            }
            set
            {
                _tuneBandWidthKHz = value;

                OnPropertyChanged(nameof(TuneBandWidthKHz));
            }
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
            OnPropertyChanged(nameof(FrequencyKHz));
            OnPropertyChanged(nameof(FrequencyWholePartMHz));
            OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));

            OnPropertyChanged(nameof(DeliverySystem));
            OnPropertyChanged(nameof(DeliverySystemCaption));

            OnPropertyChanged(nameof(TuningProgress));
            OnPropertyChanged(nameof(TuningInProgress));
            OnPropertyChanged(nameof(TuningProgressVisible));
            OnPropertyChanged(nameof(TuningProgressCaption));
            OnPropertyChanged(nameof(State));

            OnPropertyChanged(nameof(DVBTTuning));
            OnPropertyChanged(nameof(DVBT2Tuning));

            OnPropertyChanged(nameof(FrequencyFromKHz));
            OnPropertyChanged(nameof(FrequencyToKHz));
            OnPropertyChanged(nameof(FrequencyFromMHz));
            OnPropertyChanged(nameof(FrequencyToMHz));

            OnPropertyChanged(nameof(SignalProgressCaption));
            OnPropertyChanged(nameof(SignalProgress));

            OnPropertyChanged(nameof(Channels));
            OnPropertyChanged(nameof(SelectedChannel));

            OnPropertyChanged(nameof(StartButtonVisible));
            OnPropertyChanged(nameof(StopButtonVisible));
            OnPropertyChanged(nameof(BackButtonVisible));
            OnPropertyChanged(nameof(FinishButtonVisible));

            OnPropertyChanged(nameof(TunedMultiplexesCount));
            OnPropertyChanged(nameof(TunedChannelsCount));
            OnPropertyChanged(nameof(TunedNewChannelsCount));
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

        public string DeliverySystemCaption
        {
            get
            {
                return DeliverySystem == 0 ? "DVBT" : "DVBT2";
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
                return _DVBTTuning;
            }
            set
            {
                _DVBTTuning = value;

                NotifyChange();
            }
        }

        public bool DVBT2Tuning
        {
            get
            {
                return _DVBT2Tuning;
            }
            set
            {
                _DVBT2Tuning = value;

                NotifyChange();
            }
        }

        public long FrequencyFromKHz
        {
            get
            {
                return _frequencyFromKHz;
            }
            set
            {
                _frequencyFromKHz = value;
                NotifyChange();
            }
        }

        public long FrequencyToKHz
        {
            get
            {
                return _frequencyToKHz;
            }
            set
            {
                _frequencyToKHz = value;
                NotifyChange();
            }
        }

        public long FrequencyFromMHz
        {
            get
            {
                return _frequencyFromKHz / 1000;
            }
        }

        public long FrequencyToMHz
        {
            get
            {
                return _frequencyToKHz / 1000;
            }
        }

        public double TuningProgress
        {
            get
            {
                var onePerc = (FrequencyToKHz - FrequencyFromKHz) / 100.0;
                if (onePerc == 0)
                    return 0.0;

                var perc = (_actualTunningFreqKHz - FrequencyFromKHz) / onePerc;

                if (DVBTTuning && DVBT2Tuning)
                {
                    perc = perc / 2;

                    if (_actualTuningDVBTType == 1)
                    {
                        perc += 50;
                    }
                }

                if (perc < 0)
                    return 0.0;

                if (perc > 100)
                    return 100.0;

                return perc / 100.0;
            }
        }

        public string TuningProgressCaption
        {
            get
            {
                return (TuningProgress * 100).ToString("N0") + " %";
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
                return (_signalProgress * 100).ToString("N0") + "%";
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
    }
}

