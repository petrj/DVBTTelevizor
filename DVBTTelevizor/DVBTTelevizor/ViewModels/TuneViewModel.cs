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

        private bool _tuningAborted = false;
        private double _signalStrengthProgress = 0;
        private TuneState _tuneState = TuneState.TuningInProgress;

        private bool _DVBTTuning = true;
        private bool _DVBT2Tuning = true;

        private long _actualTunningFreqKHz = -1;
        private long _actualTuningDVBTType = -1;

        public const long AutoTuningMinFrequencyKHzDefaultValue = 174000;  // 174.0 MHz - VHF high-band (band III) channel 7
        public const long AutoTuningMaxFrequencyKHzDefaultValue = 858000;  // 858.0 MHz - UHF band channel 69

        public const long BandWidthMinKHz = 1000;
        public const long BandWidthMaxKHz = 32000;
        public const long BandWidthDefaultKHz = 8000;

        public long FrequencyKHzDefaultValue { get; set; } = 470000;

        public long _autoTuningMinFrequencyKHz { get; set; } = AutoTuningMinFrequencyKHzDefaultValue;
        public long _autoTuningMaxFrequencyKHz { get; set; } = AutoTuningMaxFrequencyKHzDefaultValue;

        protected long _tuneBandWidthKHz = BandWidthDefaultKHz;
        protected long _tuningFrequencyKHz { get; set; }

        public long _autoTuningFrequencyFromKHz { get; set; } = AutoTuningMinFrequencyKHzDefaultValue;
        public long _autoTuningFrequencyToKHz { get; set; } = AutoTuningMaxFrequencyKHzDefaultValue;

        private DVBTChannel _selectedChannel;

        public ObservableCollection<DVBTChannel> TunedChannels { get; set; } = new ObservableCollection<DVBTChannel>();
        private ObservableCollection<DVBTChannel> _channels = null;

        public enum TuneState
        {
            TuningInProgress = 1,
            TuneFinishedOK = 2,
            TuneFailed = 3
        }

        public TuneViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
         : base(loggingService, dialogService, driver, config)
        {
            _channelService = channelService;
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

        public long AutoTuningMinFrequencyKHz
        {
            get
            {
                return _autoTuningMinFrequencyKHz;
            }
            set
            {
                _autoTuningMinFrequencyKHz = value;

                OnPropertyChanged(nameof(AutoTuningMinFrequencyKHz));
            }
        }

        public long AutoTuningMaxFrequencyKHz
        {
            get
            {
                return _autoTuningMaxFrequencyKHz;
            }
            set
            {
                _autoTuningMaxFrequencyKHz = value;

                OnPropertyChanged(nameof(AutoTuningMaxFrequencyKHz));
            }
        }

        public long AutoTuningFrequencyFromKHz
        {
            get
            {
                return _autoTuningFrequencyFromKHz;
            }
            set
            {
                _autoTuningFrequencyFromKHz = value;

                OnPropertyChanged(nameof(AutoTuningFrequencyFromKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHzCaption));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHzCaption));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHz));
            }
        }

        public long AutoTuningFrequencyToKHz
        {
            get
            {
                return _autoTuningFrequencyToKHz;
            }
            set
            {
                _autoTuningFrequencyToKHz = value;

                OnPropertyChanged(nameof(AutoTuningFrequencyFromKHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToKHz));
                OnPropertyChanged(nameof(TuningFrequencyBandWidthMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyFromMHz));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHzCaption));
                OnPropertyChanged(nameof(AutoTuningFrequencyToMHz));
            }
        }

        public string AutoTuningFrequencyToMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyToKHz / 1000.0, 1).ToString();
            }
        }

        public long TuningFrequencyKHz
        {
            get
            {
                return _tuningFrequencyKHz;
            }
            set
            {
                _tuningFrequencyKHz = value;

                OnPropertyChanged(nameof(TuningFrequencyKHz));
                OnPropertyChanged(nameof(TuningFrequencyMHz));
                OnPropertyChanged(nameof(TuningFrequencyMHzCaption));
            }
        }

        public string TuningFrequencyMHz
        {
            get
            {
                return (TuningFrequencyKHz / 1000.0).ToString("N3");
            }
        }

        public string TuningFrequencyMHzCaption
        {
            get
            {
                return TuningFrequencyMHz + " MHz";
            }
        }

        public string TuningFrequencyBandWidthMHz
        {
            get
            {
                return BandWidthMHz + " MHz";
            }
        }

        public string AutoTuningFrequencyFromMHz
        {
            get
            {
                return Math.Round(AutoTuningFrequencyFromKHz / 1000.0, 1).ToString();
            }
        }

        public string AutoTuningFrequencyFromMHzCaption
        {
            get
            {
                return AutoTuningFrequencyFromMHz + " MHz";
            }
        }

        public string AutoTuningFrequencyToMHzCaption
        {
            get
            {
                return AutoTuningFrequencyToMHz + " MHz";
            }
        }

        public string BandWidthMHz
        {
            get
            {
                return Math.Round(TuneBandWidthKHz / 1000.0, 1).ToString();
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
                return _tuneBandWidthKHz;
            }
            set
            {
                _tuneBandWidthKHz = value;

                OnPropertyChanged(nameof(TuneBandWidthKHz));
                OnPropertyChanged(nameof(BandWidthMHz));
                OnPropertyChanged(nameof(TuningFrequencyBandWidthMHz));
            }
        }

        public string TuningLabel
        {
            get
            {
                if (State == TuneState.TuningInProgress)
                {
                    var freqMhz = (_actualTunningFreqKHz / 1000.0).ToString("N3");
                    var t = _actualTuningDVBTType == 0 ? "DVBT" : "DVBT2";
                    return $"Tuning freq #{freqMhz} MHz {t})";
                }

                return String.Empty;
            }
        }

        public double AutomaticTuningProgress
        {
            get
            {
                var onePerc = (AutoTuningFrequencyToKHz - AutoTuningFrequencyFromKHz) / 100.0;
                if (onePerc == 0)
                    return 0.0;

                var perc = (_actualTunningFreqKHz - AutoTuningFrequencyFromKHz) / onePerc;
                return perc / 100.0;
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
                OnPropertyChanged(nameof(AbortButtonVisible));
                OnPropertyChanged(nameof(TuningAborted));
            }
        }

        public bool TuningAborted
        {
            get
            {
                return _tuningAborted;
            }
            set
            {
                _tuningAborted = value;

                OnPropertyChanged(nameof(TuningAborted));
                OnPropertyChanged(nameof(AbortButtonVisible));
            }
        }

        public bool AbortButtonVisible
        {
            get
            {
                return TuningInProgress && !TuningAborted;
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
                return (State == TuneState.TuneFinishedOK)
                         ||
                       (State == TuneState.TuneFailed);
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

        private async Task AbortTune()
        {
            TuningAborted = true;
            MessagingCenter.Send("FinishButton", BaseViewModel.MSG_UpdateTunePageFocus);
        }


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
                    _actualTunningFreqKHz = AutoTuningFrequencyFromKHz;

                    do
                    {
                        //await Tune(_actualTunningFreqKHz, TuneBandWidthKHz * 1000, dvbtTypeIndex);

                        _loggingService.Info($"Tuning freq. {_actualTunningFreqKHz}");

                        _actualTunningFreqKHz += TuneBandWidthKHz;

                        OnPropertyChanged(nameof(TuningLabel));
                        OnPropertyChanged(nameof(TuningInProgress));

                        if (TuningAborted)
                        {
                            return;
                        }

                    } while (_actualTunningFreqKHz < AutoTuningFrequencyToKHz);
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


                if (TuningAborted)
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
