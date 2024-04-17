using Android.Preferences;
using LoggerService;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class TuneViewModel : BaseViewModel
    {
        private ChannelService _channelService;
        private DVBTTelevizorConfiguration _config;

        private double _signalStrengthProgress = 0;
        private TuneState _tuneState = TuneState.TuningInProgress;

        private long _actualTunningFreqKHz = -1;
        private long _actualTuningDVBTType = -1;

        public const long FrequencyMinDefaultKHz = 174000;  // 174.0 MHz - VHF high-band (band III) channel 7
        public const long FrequencyMaxDefaultKHz = 858000;  // 858.0 MHz - UHF band channel 69

        public long FrequencyDefaultKHz { get; set; } = 474000;
        public long FrequencyFromDefaultKHz { get; set; } = 474000;
        public long FrequencyToDefaultKHz { get; set; } = 852000;

        public const long BandWidthMinKHz = 1000;
        public const long BandWidthMaxKHz = 64000;
        public const long BandWidthDefaultKHz = 8000;

        public long _frequencyMinKHz { get; set; } = FrequencyMinDefaultKHz;
        public long _frequencyMaxKHz { get; set; } = FrequencyMaxDefaultKHz;

        protected long _bandWidthKHz = BandWidthDefaultKHz;

        protected long _frequencyKHz { get; set; }
        public long _frequencyFromKHz { get; set; } = FrequencyMinDefaultKHz;
        public long _frequencyToKHz { get; set; } = FrequencyMaxDefaultKHz;

        private DVBTChannel _selectedChannel;

        public ObservableCollection<DVBTChannel> TunedChannels { get; set; } = new ObservableCollection<DVBTChannel>();
        private ObservableCollection<DVBTChannel> _savedChannels = null;
        private int _newTunedChannelsCount = 0;

        public Command AbortTuneCommand { get; set; }
        public Command FinishTuningCommand { get; set; }

        public enum TuneState
        {
            TuningInProgress = 1,
            TuneFinishedOK = 2,
            TuneFailed = 3,
            TuneAborted = 4
        }

        public TuneViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
         : base(loggingService, dialogService, driver, config)
        {
            _channelService = channelService;
            _config = config;

            AbortTuneCommand = new Command(() =>
            {
                State = TuneState.TuneAborted;
                MessagingCenter.Send("FinishButton", BaseViewModel.MSG_UpdateTuningPageFocus);
            });

            FinishTuningCommand = new Command(() =>
            {
                MessagingCenter.Send(string.Empty, BaseViewModel.MSG_CloseTuningPage);
            });

            driver.StatusChanged += Driver_StatusChanged;
        }

        private void Driver_StatusChanged(object sender, EventArgs e)
        {
            if (e is DVBTTelevizor.StatusChangedEventArgs statusArgs &&
               statusArgs.Status != null &&
               statusArgs.Status is DVBTStatus status)
            {
                if (status.SuccessFlag)
                {
                    SignalStrengthProgress = status.rfStrengthPercentage / 100.0;
                }
                else
                {
                    SignalStrengthProgress = 0;
                }
            }
        }

        public bool ManualTuning
        {
            get
            {
                return _config.ManualTuning;
            }
            set
            {
                _config.ManualTuning = value;

                OnPropertyChanged(nameof(ManualTuning));
                OnPropertyChanged(nameof(AutomaticTuning));
            }
        }

        public bool FastTuning
        {
            get
            {
                return _config.FastTuning;
            }
            set
            {
                _config.FastTuning = value;

                OnPropertyChanged(nameof(FastTuning));
            }
        }

        public bool AutomaticTuning
        {
            get
            {
                return !ManualTuning;
            }
            set
            {
                ManualTuning = !value;

                OnPropertyChanged(nameof(AutomaticTuning));
                OnPropertyChanged(nameof(ManualTuning));
            }
        }

        public int TuneModeIndex
        {
            get
            {
                return ManualTuning ? 1 : 0;
            }
            set
            {
                ManualTuning = value == 1;

                OnPropertyChanged(nameof(ManualTuning));
                OnPropertyChanged(nameof(TuneModeIndex));
            }
        }

        public bool DVBTTuning
        {
            get
            {
                return !_config.DVBTTuningDisabled;
            }
            set
            {
                _config.DVBTTuningDisabled = !value;

                OnPropertyChanged(nameof(DVBTTuning));
            }
        }

        public bool DVBT2Tuning
        {
            get
            {
                return !_config.DVBT2TuningDisabled;
            }
            set
            {
                _config.DVBT2TuningDisabled = !value;

                OnPropertyChanged(nameof(DVBT2Tuning));
            }
        }

        public long FrequencyMinKHz
        {
            get
            {
                return _frequencyMinKHz;
            }
            set
            {
                _frequencyMinKHz = value;

                OnPropertyChanged(nameof(FrequencyMinKHz));

                OnPropertyChanged(nameof(FrequencyFromKHz));
                OnPropertyChanged(nameof(FrequencyToKHz));
                OnPropertyChanged(nameof(FrequencyKHz));
            }
        }

        public long FrequencyMaxKHz
        {
            get
            {
                return _frequencyMaxKHz;
            }
            set
            {
                _frequencyMaxKHz = value;

                OnPropertyChanged(nameof(FrequencyMaxKHz));

                OnPropertyChanged(nameof(FrequencyFromKHz));
                OnPropertyChanged(nameof(FrequencyToKHz));
                OnPropertyChanged(nameof(FrequencyKHz));
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

                OnPropertyChanged(nameof(FrequencyFromKHz));
                OnPropertyChanged(nameof(FrequencyToKHz));
                OnPropertyChanged(nameof(FrequencyFromMHz));
                OnPropertyChanged(nameof(FrequencyFromMHzCaption));
                OnPropertyChanged(nameof(FrequencyToMHzCaption));
                OnPropertyChanged(nameof(FrequencyToMHz));
                OnPropertyChanged(nameof(FrequencyFromWholePartMHz));
                OnPropertyChanged(nameof(FrequencyFromDecimalPartMHzCaption));
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

                OnPropertyChanged(nameof(FrequencyFromKHz));
                OnPropertyChanged(nameof(FrequencyToKHz));
                OnPropertyChanged(nameof(BandWidthMHzCaption));
                OnPropertyChanged(nameof(FrequencyFromMHz));
                OnPropertyChanged(nameof(FrequencyToMHzCaption));
                OnPropertyChanged(nameof(FrequencyToMHz));
                OnPropertyChanged(nameof(FrequencyToWholePartMHz));
                OnPropertyChanged(nameof(FrequencyToDecimalPartMHzCaption));
            }
        }

        public double FrequencyToMHz
        {
            get
            {
                return FrequencyToKHz / 1000.0;
            }
        }

        public long FrequencyKHz
        {
            get
            {
                return _frequencyKHz;
            }
            set
            {
                _frequencyKHz = value;

                _config.FrequencyKHz = _frequencyKHz;

                OnPropertyChanged(nameof(FrequencyKHz));
                OnPropertyChanged(nameof(FrequencyMHz));
                OnPropertyChanged(nameof(FrequencyMHzCaption));
                OnPropertyChanged(nameof(FrequencyWholePartMHz));
                OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));
            }
        }

        public double FrequencyMHz
        {
            get
            {
                return FrequencyKHz / 1000.0;
            }
        }

        public string FrequencyMHzCaption
        {
            get
            {
                return FrequencyMHz.ToString("N3") + " MHz";
            }
        }


        public double BandWidthMHz
        {
            get
            {
                return TuneBandWidthKHz / 1000.0;
            }
        }

        public string BandWidthMHzCaption
        {
            get
            {
                return BandWidthMHz.ToString("N3") + " MHz";
            }
        }

        public long BandWidthWholePartMHz
        {
            get
            {
                return Convert.ToInt64(Math.Floor(TuneBandWidthKHz / 1000.0));
            }
        }

        public string BandWidthDecimalPartMHzCaption
        {
            get
            {
                var part = (TuneBandWidthKHz / 1000.0) - BandWidthWholePartMHz;
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000} MHz";
            }
        }

        public double FrequencyFromMHz
        {
            get
            {
                return FrequencyFromKHz / 1000.0;
            }
        }

        public string FrequencyFromMHzCaption
        {
            get
            {
                return FrequencyFromMHz.ToString("N3") + " MHz";
            }
        }

        public string FrequencyToMHzCaption
        {
            get
            {
                return FrequencyToMHz.ToString("N3") + " MHz";
            }
        }

        public long FrequencyFromWholePartMHz
        {
            get
            {
                return Convert.ToInt64(Math.Floor(FrequencyFromKHz / 1000.0));
            }
        }

        public string FrequencyFromDecimalPartMHzCaption
        {
            get
            {
                var part = (FrequencyFromKHz / 1000.0) - FrequencyFromWholePartMHz;
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000} MHz";
            }
        }

        public long FrequencyToWholePartMHz
        {
            get
            {
                return Convert.ToInt64(Math.Floor(FrequencyToKHz / 1000.0));
            }
        }

        public string FrequencyToDecimalPartMHzCaption
        {
            get
            {
                var part = (FrequencyToKHz / 1000.0) - FrequencyToWholePartMHz;
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000} MHz";
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

        public DVBTChannel FirstTunedChannel
        {
            get
            {
                if (TunedChannels.Count == 0)
                {
                    return null;
                }

                return TunedChannels[0];
            }
        }

        public DVBTChannel LastTunedChannel
        {
            get
            {
                if (TunedChannels.Count == 0)
                {
                    return null;
                }

                return TunedChannels[TunedChannels.Count-1];
            }
        }

        public DVBTChannel SelectedChannel
        {
            get
            {
                return _selectedChannel;
            }
            set
            {
                _selectedChannel = value;

                OnPropertyChanged(nameof(SelectedChannel));
            }
        }

        public int SelectNextTunedChannel()
        {
            if (TunedChannels == null || TunedChannels.Count == 0)
            {
                SelectedChannel = null;
                return -1;
            }

            if (SelectedChannel == null)
            {
                SelectedChannel = TunedChannels[0];
                return 1;
            }

            for (var i = 0; i < TunedChannels.Count; i++)
            {
                if (TunedChannels[i] == SelectedChannel)
                {
                    if (i == TunedChannels.Count -1 )
                    {
                        return 0;
                    }

                    SelectedChannel = TunedChannels[i + 1];

                    return 1;
                }
            }

            return -1;
        }

        public int SelectPreviousChannel()
        {
            if (TunedChannels == null || TunedChannels.Count == 0)
            {
                SelectedChannel = null;
                return -1;
            }

            if (SelectedChannel == null)
            {
                SelectedChannel = TunedChannels[TunedChannels.Count - 1];
                return 1;
            }

            for (var i = 0; i < TunedChannels.Count; i++)
            {
                if (TunedChannels[i] == SelectedChannel)
                {
                    if (i == 0)
                    {
                        return 0;
                    }

                    SelectedChannel = TunedChannels[i - 1];

                    return 1;
                }
            }

            return -1;
        }

        public long TuneBandWidthKHz
        {
            get
            {
                return _bandWidthKHz;
            }
            set
            {
                _bandWidthKHz = value;

                _config.BandWidthKHz = _bandWidthKHz;

                OnPropertyChanged(nameof(TuneBandWidthKHz));
                OnPropertyChanged(nameof(BandWidthMHz));
                OnPropertyChanged(nameof(BandWidthMHzCaption));

                OnPropertyChanged(nameof(BandWidthMHzCaption));
                OnPropertyChanged(nameof(BandWidthWholePartMHz));
                OnPropertyChanged(nameof(BandWidthDecimalPartMHzCaption));
            }
        }

        public string DeliverySystem
        {
            get
            {
                return _actualTuningDVBTType == 0 ? "DVBT" : "DVBT2";
            }
        }

        public int TunedChannelsCount
        {
            get
            {
                return TunedChannels.Count;
            }
        }

        public int NewTunedChannelsCount
        {
            get
            {
                return _newTunedChannelsCount;
            }
        }

        public int TunedMultiplexesCount
        {
            get
            {
                var dict = new Dictionary<long, int>();
                foreach (var channel in TunedChannels)
                {
                    if (!dict.ContainsKey(channel.Frequency))
                    {
                        dict[channel.Frequency] = 0;
                    }
                    dict[channel.Frequency]++;
                }
                return dict.Count;
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
                        perc +=50;
                    }
                }

                if (perc<0)
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

        public string ActualTuningFrequencyWholePartMHz
        {
            get
            {
                if (_actualTunningFreqKHz < 0)
                    return string.Empty;

                return Convert.ToInt64(Math.Floor(_actualTunningFreqKHz / 1000.0)).ToString();
            }
        }

        public string ActualTuningState
        {
            get
            {
                switch (State)
                {
                    case TuneState.TuneFinishedOK:
                        return "Finished";
                    case TuneState.TuneFailed:
                        return "Failed";
                    case TuneState.TuneAborted:
                        return "Aborted";
                    default:
                        return string.Empty;
                }
            }
        }

        public string ActualTuningFrequencyDecimalPartMHzCaption
        {
            get
            {
                if (_actualTunningFreqKHz < 0)
                    return string.Empty;

                var part = (_actualTunningFreqKHz / 1000.0) - Convert.ToInt64(Math.Floor(_actualTunningFreqKHz / 1000.0));
                var part1000 = Convert.ToInt64(part * 1000).ToString().PadLeft(3, '0');
                return $".{part1000} MHz";
            }
        }

        public TuneState State
        {
            get
            {
                return _tuneState;
            }
            set
            {
                _tuneState = value;

                UpdateTuningProperties();
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

                OnPropertyChanged(nameof(SignalStrengthProgress));
                OnPropertyChanged(nameof(SignalStrengthProgressCaption));
            }
        }

        public string SignalStrengthProgressCaption
        {
            get
            {
                return (_signalStrengthProgress * 100).ToString("N0") + " %";
            }
        }

        public bool TuningInProgress
        {
            get
            {
                return State == TuneState.TuningInProgress;
            }
        }

        public bool AddChannelsVisible
        {
            get
            {
                return (
                            (State == TuneState.TuneFinishedOK)
                            ||
                            (State == TuneState.TuneFailed)
                       )
                        &&
                            TunedChannels.Count > 0;
            }
        }

        public bool TuningFinished
        {
            get
            {
                return (State != TuneState.TuningInProgress);
            }
        }

        public bool TuningNotInProgress
        {
            get
            {
                return !TuningInProgress;
            }
        }

        public static long ParseFreqMHzToKHz(string freqMHz)
        {
            decimal freqMHzDecimal = -1;
            var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            if (decimal.TryParse(freqMHz.Replace(".", sep).Replace(",", sep), out freqMHzDecimal))
            {
                return Convert.ToInt64(freqMHzDecimal * 1000);
            }
            else
            {
                return -1;
            }
        }

        private bool ValidFrequency(long freqKHz)
        {
            if (freqKHz == default)
                return false;

            if (freqKHz < FrequencyMinKHz || freqKHz > FrequencyMaxKHz)
            {
                return false;
            }

            return true;
        }

        public async Task SetFrequencies()
        {
            _loggingService.Info("SetChannelsRange");

            try
            {
                // bandwidth
                if (_config.BandWidthKHz != default &&
                    _config.BandWidthKHz >= BandWidthMinKHz &&
                    _config.BandWidthKHz <= BandWidthMaxKHz)
                {
                    TuneBandWidthKHz = _config.BandWidthKHz;
                }

                try
                {
                    FrequencyMinKHz = FrequencyMinDefaultKHz;
                    FrequencyMaxKHz = FrequencyMaxDefaultKHz;

                    var cap = await _driver.GetCapabalities();

                    // setting min/max frequencies from device
                    if (cap.SuccessFlag)
                    {
                        FrequencyMinKHz = cap.minFrequency / 1000;
                        FrequencyMaxKHz = cap.maxFrequency / 1000;
                    }
                } catch
                {
                }

                // setting default frequencies according to min/max
                if (!ValidFrequency(FrequencyDefaultKHz))
                {
                    FrequencyDefaultKHz = FrequencyMinKHz + TuneBandWidthKHz/2;
                }
                if (!ValidFrequency(FrequencyFromDefaultKHz))
                {
                    FrequencyFromDefaultKHz = FrequencyMinKHz + TuneBandWidthKHz / 2;
                }
                if (!ValidFrequency(FrequencyToDefaultKHz))
                {
                    FrequencyToDefaultKHz = FrequencyMaxKHz;
                }

                // loading frequencies from configuration
                if (ValidFrequency(Config.FrequencyFromKHz))
                {
                    FrequencyFromKHz = Config.FrequencyFromKHz;
                } else
                {
                    FrequencyFromKHz = FrequencyFromDefaultKHz;
                }
                if (ValidFrequency(Config.FrequencyToKHz))
                {
                    FrequencyToKHz = Config.FrequencyToKHz;
                } else
                {
                    FrequencyToKHz = FrequencyToDefaultKHz;
                }

                if (ValidFrequency(Config.FrequencyKHz))
                {
                    FrequencyKHz = Config.FrequencyKHz;
                } else
                {
                    FrequencyKHz = FrequencyDefaultKHz;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
            finally
            {

            }
        }

        public void UpdateTuningProperties()
        {
            OnPropertyChanged(nameof(TuningFinished));
            OnPropertyChanged(nameof(TuningInProgress));
            OnPropertyChanged(nameof(ActualTuningState));
            OnPropertyChanged(nameof(ActualTuningFrequencyWholePartMHz));
            OnPropertyChanged(nameof(ActualTuningFrequencyDecimalPartMHzCaption));
            OnPropertyChanged(nameof(DeliverySystem));
            OnPropertyChanged(nameof(TuningInProgress));
            OnPropertyChanged(nameof(TuningProgress));
            OnPropertyChanged(nameof(TuningProgressCaption));
            OnPropertyChanged(nameof(SignalStrengthProgress));
            OnPropertyChanged(nameof(SignalStrengthProgressCaption));
            OnPropertyChanged(nameof(TunedChannelsCount));
            OnPropertyChanged(nameof(NewTunedChannelsCount));
            OnPropertyChanged(nameof(TunedMultiplexesCount));
        }

        public async Task Tune()
        {
            try
            {
                _loggingService.Info("Starting tuning");

                _savedChannels = await _channelService.LoadChannels();

                for (var dvbtTypeIndex = 0; dvbtTypeIndex <= 1; dvbtTypeIndex++)
                {
                    if (!DVBTTuning && dvbtTypeIndex == 0)
                        continue;
                    if (!DVBT2Tuning && dvbtTypeIndex == 1)
                        continue;

                    _actualTuningDVBTType = dvbtTypeIndex;
                    _actualTunningFreqKHz = FrequencyFromKHz;

                    UpdateTuningProperties();

                    do
                    {
                        _loggingService.Info($"Tuning freq. {_actualTunningFreqKHz}");

                        await Tune(_actualTunningFreqKHz * 1000, TuneBandWidthKHz * 1000, dvbtTypeIndex);

                        if (FrequencyToKHz != FrequencyFromKHz)
                        {
                            _actualTunningFreqKHz += TuneBandWidthKHz;
                        }

                        UpdateTuningProperties();

                        if (State == TuneState.TuneAborted)
                        {
                            return;
                        }

                    } while (_actualTunningFreqKHz < FrequencyToKHz);
                }

                State = TuneState.TuneFinishedOK;
                SignalStrengthProgress = 0;
                MessagingCenter.Send("FinishButton", BaseViewModel.MSG_UpdateTuningPageFocus);

            } catch (Exception ex)
            {
                _loggingService.Error(ex);
                State = TuneState.TuneFailed;
            }
        }

        private async Task Tune(long freq, long bandWidth, int dvbtTypeIndex)
        {
            try
            {
                SignalStrengthProgress = 0;

                var tuneResult = await _driver.TuneEnhanced(freq, bandWidth, dvbtTypeIndex, FastTuning);

                switch (tuneResult.Result)
                {
                    case SearchProgramResultEnum.Error:
                        _loggingService.Debug("Search error");
                        return;

                    case SearchProgramResultEnum.NoSignal:
                        _loggingService.Debug("No signal");
                        return;
                }

                var searchMapPIDsResult = await _driver.SearchProgramMapPIDs(false);

                switch (searchMapPIDsResult.Result)
                {
                    case SearchProgramResultEnum.Error:
                        _loggingService.Debug("Search error");

                        return;

                    case SearchProgramResultEnum.NoSignal:
                        _loggingService.Debug("No signal");

                        return;

                    case SearchProgramResultEnum.NoProgramFound:
                        _loggingService.Debug("No program found");

                        return;
                }

                var mapPIDs = new List<long>();

                foreach (var sd in searchMapPIDsResult.ServiceDescriptors)
                {
                    mapPIDs.Add(sd.Value);
                }
                _loggingService.Debug($"Program MAP PIDs found: {String.Join(",", mapPIDs)}");

                if (State == TuneState.TuneAborted)
                {
                    _loggingService.Debug($"Tuning aborted");
                    return;
                }

                var totalChannelsAddedCount = 0;

                // searching PIDs not neccessary from version 14
                foreach (var serviceDescriptor in searchMapPIDsResult.ServiceDescriptors)
                {
                    var ch = new DVBTChannel();
                    ch.ProgramMapPID = serviceDescriptor.Value;
                    ch.Name = serviceDescriptor.Key.ServiceName;
                    ch.ProviderName = serviceDescriptor.Key.ProviderName;
                    ch.Frequency = freq;
                    ch.Bandwdith = bandWidth;
                    ch.Number = String.Empty;
                    ch.DVBTType = dvbtTypeIndex;
                    ch.Type = (ServiceTypeEnum)serviceDescriptor.Key.ServisType;

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        TunedChannels.Add(ch);
                        OnPropertyChanged(nameof(TunedChannelsCount));
                        OnPropertyChanged(nameof(NewTunedChannelsCount));
                        OnPropertyChanged(nameof(TunedMultiplexesCount));
                    });

                    _loggingService.Debug($"Found channel \"{serviceDescriptor.Key.ServiceName}\"");

                    // automatically adding new tuned channel if does not exist
                    if (!ConfigViewModel.ChannelExists(_savedChannels, ch.FrequencyAndMapPID))
                    {
                        ch.Number = ConfigViewModel.GetNextChannelNumber(_savedChannels).ToString();

                        _savedChannels.Add(ch);

                        await _channelService.SaveChannels(_savedChannels);
                        totalChannelsAddedCount++;
                        _newTunedChannelsCount++;
                    }
                }

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
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
