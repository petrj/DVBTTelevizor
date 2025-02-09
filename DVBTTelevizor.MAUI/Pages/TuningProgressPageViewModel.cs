using LoggerService;
using Microsoft.Maui;
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

        private int _actualTuningDVBTType = 1; // 0 .. DVBT, 1 .. DVBT2
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

        public void StartTune()
        {
            if (State == TuneStateEnum.Inactive)
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

            _tuneState = TuneStateEnum.InProgress;

            NotifyChange();
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
                return _frequencyFromKHz;
            }
            set
            {
                _frequencyFromKHz = value;
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
    }
}

