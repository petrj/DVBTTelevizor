using LoggerService;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class TuneViewModel : BaseViewModel
    {
        ChannelService _channelService;

        private bool _manualTuning = false;

          private double _signalStrengthProgress = 0;
        private TuneState _tuneState = TuneState.TuningInProgress;

        private bool _DVBTTuning = true;
        private bool _DVBT2Tuning = true;

        private long _actualTunningFreqKHz = -1;
        private long _actualTuningDVBTType = -1;

        public const long FrequencyMinDefaultKHz = 174000;  // 174.0 MHz - VHF high-band (band III) channel 7
        public const long FrequencyMaxDefaultKHz = 858000;  // 858.0 MHz - UHF band channel 69

        public long FrequencyDefaultKHz { get; set; } = 470000;

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
        private ObservableCollection<DVBTChannel> _channels = null;

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

            AbortTuneCommand = new Command(() =>
            {
                State = TuneState.TuneAborted;
                MessagingCenter.Send("FinishButton", BaseViewModel.MSG_UpdateTuningPageFocus);
            });

            FinishTuningCommand = new Command(() =>
            {
                MessagingCenter.Send(string.Empty, BaseViewModel.MSG_CloseTuningPage);
            });
        }

        public bool ManualTuning
        {
            get
            {
                return _manualTuning;
            }
            set
            {
                _manualTuning = value;

                OnPropertyChanged(nameof(ManualTuning));
                OnPropertyChanged(nameof(AutomaticTuning));
            }
        }

        public bool AutomaticTuning
        {
            get
            {
                return !_manualTuning;
            }
            set
            {
                _manualTuning = !value;

                OnPropertyChanged(nameof(AutomaticTuning));
                OnPropertyChanged(nameof(ManualTuning));
            }
        }

        public int TuneModeIndex
        {
            get
            {
                return _manualTuning ? 1 : 0;
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
                return _DVBTTuning;
            }
            set
            {
                _DVBTTuning = value;

                OnPropertyChanged(nameof(DVBTTuning));
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

                OnPropertyChanged(nameof(FrequencyKHz));
                OnPropertyChanged(nameof(FrequencyMHz));
                OnPropertyChanged(nameof(FrequencyMHzCaption));
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

        public string BandWidthMHzCaption
        {
            get
            {
                return BandWidthMHz.ToString("N3") + " MHz";
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

        public double BandWidthMHz
        {
            get
            {
                return TuneBandWidthKHz / 1000.0;
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

        public long TuneBandWidthKHz
        {
            get
            {
                return _bandWidthKHz;
            }
            set
            {
                _bandWidthKHz = value;

                OnPropertyChanged(nameof(TuneBandWidthKHz));
                OnPropertyChanged(nameof(BandWidthMHz));
                OnPropertyChanged(nameof(BandWidthMHzCaption));
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

        public string TuningLabel
        {
            get
            {
                switch (State)
                {
                    case TuneState.TuningInProgress:
                        var freqMhz = (_actualTunningFreqKHz / 1000.0).ToString("N3");
                        return $"Tuning {freqMhz} MHz";

                    case TuneState.TuneFinishedOK:
                        return $"Tuning finished";

                    case TuneState.TuneFailed:
                        return $"Tuning failed";

                    case TuneState.TuneAborted:
                        return $"Tuning aborted";

                    default:
                        return String.Empty;
                }
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

        public TuneState State
        {
            get
            {
                return _tuneState;
            }
            set
            {
                _tuneState = value;

                OnPropertyChanged(nameof(TuningFinished));
                OnPropertyChanged(nameof(TuningLabel));
                OnPropertyChanged(nameof(TuningInProgress));
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

        public async Task SetChannelsRange()
        {
            _loggingService.Info("SetChannelsRange");

            try
            {
                var cap = await _driver.GetCapabalities();

                if (!cap.SuccessFlag)
                {
                    throw new Exception("Response not success");
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
        /*
        private async Task Tune()
        {
            _loggingService.Info($"Tuning");

            //MessagingCenter.Send("AbortButton", BaseViewModel.MSG_UpdateTunePageFocus);
            State = TuneState.TuningInProgress;

            TunedChannels.Clear();

            _channels = await _channelService.LoadChannels();
            if (_channels == null) _channels = new ObservableCollection<DVBTChannel>();

            OnPropertyChanged(nameof(TuningLabel));
            OnPropertyChanged(nameof(AutomaticTuningProgress));

            TuningAborted = false;

            try
            {
                if (ManualTuning)
                {
                    long freqHz = TuningFrequencyKHz * 1000;
                    long bandWidthHz = TuneBandWidthKHz * 1000;

                    //var ch = Convert.ToInt32((Convert.ToInt64(TuneFrequency) - 474 + 8 * 21) / 8);

                    _actualTunningFreqKHz = TuningFrequencyKHz;
                    _actualTuningDVBTType = 0;

                    OnPropertyChanged(nameof(TuningLabel));

                    if (DVBTTuning) await Tune(freqHz, bandWidthHz, 0);

                    _actualTuningDVBTType = 1;
                    OnPropertyChanged(nameof(TuningLabel));

                    if (DVBT2Tuning) await Tune(freqHz, bandWidthHz, 1);
                }
                else
                {
                    await AutomaticTune();
                }

                State = TuneState.TuneFinishedOK;
            }
            catch (Exception ex)
            {
                State = TuneState.TuneFailed;
            }
            finally
            {

                OnPropertyChanged(nameof(TuningFinished));
                OnPropertyChanged(nameof(TunedChannels));
                OnPropertyChanged(nameof(AddChannelsVisible));

                OnPropertyChanged(nameof(TuningLabel));
                OnPropertyChanged(nameof(AutomaticTuningProgress));

                MessagingCenter.Send("FinishButton", BaseViewModel.MSG_UpdateTunePageFocus);
            }
        }*/

        public async Task Tune()
        {
            try
            {
                _loggingService.Info("Starting tuning");

                for (var dvbtTypeIndex = 0; dvbtTypeIndex <= 1; dvbtTypeIndex++)
                {
                    if (!DVBTTuning && dvbtTypeIndex == 0)
                        continue;
                    if (!DVBT2Tuning && dvbtTypeIndex == 1)
                        continue;

                    _actualTuningDVBTType = dvbtTypeIndex;
                    _actualTunningFreqKHz = FrequencyFromKHz;

                    do
                    {
                        //await Tune(_actualTunningFreqKHz, TuneBandWidthKHz * 1000, dvbtTypeIndex);

                        System.Threading.Thread.Sleep(300);

                        _loggingService.Info($"Tuning freq. {_actualTunningFreqKHz}");

                        _actualTunningFreqKHz += TuneBandWidthKHz;

                        SignalStrengthProgress = 0.7;

                        OnPropertyChanged(nameof(TuningLabel));
                        OnPropertyChanged(nameof(DeliverySystem));
                        OnPropertyChanged(nameof(TuningInProgress));
                        OnPropertyChanged(nameof(TuningProgress));
                        OnPropertyChanged(nameof(TuningProgressCaption));

                        if (State == TuneState.TuneAborted)
                        {
                            return;
                        }

                    } while (_actualTunningFreqKHz < FrequencyToKHz);
                }

                State = TuneState.TuneFinishedOK;
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

                //#if DEBUG
                //                await Task.Delay(1000);

                //                var channel = new DVBTChannel();
                //                channel.PIDs = "0,16,17";
                //                channel.ProgramMapPID = 5000;
                //                channel.Name = "Channel name";
                //                channel.ProviderName = "Multiplex";
                //                channel.Frequency = freq;
                //                channel.Bandwdith = bandWidth;
                //                channel.Number = String.Empty;
                //                channel.DVBTType = dvbtTypeIndex;
                //                channel.Type = ServiceTypeEnum.DigitalTelevisionService;

                //                TunedChannels.Add(channel);

                //                Device.BeginInvokeOnMainThread(() =>
                //                {
                //                    SelectedChannel = channel;
                //                });
                //#endif


                var tuneResult = await _driver.TuneEnhanced(freq, bandWidth, dvbtTypeIndex);

                switch (tuneResult.Result)
                {
                    case SearchProgramResultEnum.Error:
                        _loggingService.Debug("Search error");

                        SignalStrengthProgress = 0;
                        return;

                    case SearchProgramResultEnum.NoSignal:
                        _loggingService.Debug("No signal");

                        SignalStrengthProgress = 0;
                        return;

                    case SearchProgramResultEnum.OK:

                        SignalStrengthProgress = tuneResult.SignalPercentStrength / 100.0;
                        break;
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
                var mapPIDToServiceDescriptor = new Dictionary<long, ServiceDescriptor>();

                foreach (var sd in searchMapPIDsResult.ServiceDescriptors)
                {
                    mapPIDs.Add(sd.Value);
                    mapPIDToServiceDescriptor.Add(sd.Value, sd.Key);
                }
                _loggingService.Debug($"Program MAP PIDs found: {String.Join(",", mapPIDs)}");


                if (State == TuneState.TuneAborted)
                {
                    _loggingService.Debug($"Tuning aborted");
                    return;
                }

                // searching PIDs

                var searchProgramPIDsResult = await _driver.SearchProgramPIDs(mapPIDs);

                switch (searchProgramPIDsResult.Result)
                {
                    case SearchProgramResultEnum.Error:
                        _loggingService.Debug($"Error scanning Map PIDs");
                        break;
                    case SearchProgramResultEnum.NoSignal:
                        _loggingService.Debug("No signal");
                        break;
                    case SearchProgramResultEnum.NoProgramFound:
                        _loggingService.Debug("No program found");
                        break;
                    case SearchProgramResultEnum.OK:

                        var totalChannelsAddedCount = 0;

                        foreach (var kvp in searchProgramPIDsResult.PIDs)
                        {
                            var pids = string.Join(",", kvp.Value);
                            var sDescriptor = mapPIDToServiceDescriptor[kvp.Key];

                            var ch = new DVBTChannel();
                            ch.PIDs = pids;
                            ch.ProgramMapPID = kvp.Key;
                            ch.Name = sDescriptor.ServiceName;
                            ch.ProviderName = sDescriptor.ProviderName;
                            ch.Frequency = freq;
                            ch.Bandwdith = bandWidth;
                            ch.Number = String.Empty;
                            ch.DVBTType = dvbtTypeIndex;
                            ch.Type = (ServiceTypeEnum)sDescriptor.ServisType;

                            TunedChannels.Add(ch);

                            Device.BeginInvokeOnMainThread(() =>
                            {
                                SelectedChannel = ch;
                            });

                            _loggingService.Debug($"Found channel \"{sDescriptor.ServiceName}\"");

                            // automatically adding new tuned channel if does not exist
                            if (!ConfigViewModel.ChannelExists(_channels, ch.Frequency, ch.ProgramMapPID))
                            {
                                ch.Number = ConfigViewModel.GetNextChannelNumber(_channels).ToString();

                                _channels.Add(ch);

                                await _channelService.SaveChannels(_channels);
                                totalChannelsAddedCount++;
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

                        break;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
