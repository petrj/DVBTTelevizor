using DVBTTelevizor.Models;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class TunePageViewModel : BaseViewModel
    {
        private bool _manualTuning = true;
        private bool _tuningAborted = false;
        private string _tuneFrequency ;
        private long _tuneBandwidth = 8;

        private bool _DVBTTuning = true;
        private bool _DVBT2Tuning = true;

        ChannelService _channelService;

        DVBTFrequencyChannel _selectedFrequencyChannel = null;

        private TuneState _tuneState = TuneState.Ready;

        public long AutomaticTuningFirstChannel { get; set; } = 21;
        public long AutomaticTuningLastChannel { get; set; } = 69;
        private long AutomaticTuningActualChannel = -1;

        private double _signalStrengthProgress = 0;

        public ObservableCollection<DVBTChannel> TunedChannels { get; set;  } = new ObservableCollection<DVBTChannel>();

        public ObservableCollection<DVBTFrequencyChannel> FrequencyChannels { get; set; } = new ObservableCollection<DVBTFrequencyChannel>();

        public Command TuneCommand { get; set; }
        public Command AbortTuneCommand { get; set; }
        public Command SaveTunedChannelsCommand { get; set; }
        public Command FinishTunedCommand { get; set; }

        public enum TuneState
        {
            Ready = 0,
            TuningInProgress = 1,
            TuneFinishedOK = 2,
            TuneFailed= 3
        }

        public TunePageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
         : base(loggingService, dialogService, driver, config)
        {
            _channelService = channelService;

            TuneCommand = new Command(async () => await Tune());
            AbortTuneCommand = new Command(async () => await AbortTune());
            SaveTunedChannelsCommand = new Command(async () => await SaveTunedChannels());
            FinishTunedCommand = new Command(async () => await FinishTune());

            FillFrequencyChannels();
        }

        private void FillFrequencyChannels()
        {
            FrequencyChannels.Clear();

            for (var i=21;i<=69;i++)
            {
                var freqMhz = (474 + 8 * (i - 21));
                var fc = new DVBTFrequencyChannel()
                {
                    FrequencyMhZ = freqMhz,
                    ChannelNumber = i
                };

                FrequencyChannels.Add(fc);
            }
        }

        public DVBTFrequencyChannel SelectedFrequencyChannelItem
        {
            get
            {
                return _selectedFrequencyChannel;
            }
            set
            {
                _selectedFrequencyChannel = value;

                if (value != null)
                {
                    TuneFrequency = (_selectedFrequencyChannel.FrequencyMhZ).ToString();
                }

                OnPropertyChanged(nameof(SelectedFrequencyChannelItem));
                OnPropertyChanged(nameof(TuneFrequency));
            }
        }

        public bool TuneReady
        {
            get
            {
                return State == TuneState.Ready;
            }
        }

        public bool ManualTuneOptionsVisible
        {
            get
            {
                return ManualTuning && State == TuneState.Ready;
            }
        }

        public bool TuneOptionsVisible
        {
            get
            {
                return State == TuneState.Ready;
            }
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
                OnPropertyChanged(nameof(TuneOptionsVisible));
                OnPropertyChanged(nameof(ManualTuneOptionsVisible));                
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
                OnPropertyChanged(nameof(TuneOptionsVisible));
                OnPropertyChanged(nameof(ManualTuneOptionsVisible));
            }
        }

        public string AutomaticTuningLabel
        {
            get
            {
                if (State == TuneState.TuningInProgress)
                {
                    var freqMhz = (474 + 8 * (AutomaticTuningActualChannel - 21));
                    return $"Tuning channel {AutomaticTuningActualChannel} ({freqMhz} Mhz)";
                }

                return String.Empty;
            }            
        }

        public double AutomaticTuningProgress
        {
            get
            {
                var onePerc = (AutomaticTuningLastChannel - AutomaticTuningFirstChannel) / 100.0;
                if (onePerc == 0)
                    return 0.0;

                var perc = (AutomaticTuningActualChannel - AutomaticTuningFirstChannel) / onePerc;
                return perc / 100.0;
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

        public TuneState State
        {
            get
            {
                return _tuneState;
            }
            set
            {
                _tuneState = value;

                OnPropertyChanged(nameof(TuneReady));
                OnPropertyChanged(nameof(TuneOptionsVisible));
                OnPropertyChanged(nameof(ManualTuneOptionsVisible));
                OnPropertyChanged(nameof(AutomaticTuningInProgress));
                OnPropertyChanged(nameof(TuningInProgress));
                OnPropertyChanged(nameof(TuningNotInProgress));
                OnPropertyChanged(nameof(AddChannelsVisible));
                OnPropertyChanged(nameof(TuningFinished));
            }
        }

        public bool TuningInProgress
        {
            get
            {
                return State == TuneState.TuningInProgress;
            }
        }

        public bool AutomaticTuningInProgress
        {
            get
            {
                return State == TuneState.TuningInProgress && !ManualTuning;
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


        public string TuneFrequency
        {
            get
            {
                return _tuneFrequency;
            }
            set
            {
                _tuneFrequency = value;

                foreach (var f in FrequencyChannels)
                {
                    if (f.FrequencyMhZ.ToString() == value)
                    {
                        _selectedFrequencyChannel = f;
                        break;
                    }
                }

                OnPropertyChanged(nameof(TuneFrequency));
                OnPropertyChanged(nameof(SelectedFrequencyChannelItem));
            }
        }

        public long TuneBandwidth
        {
            get
            {
                return _tuneBandwidth;
            }
            set
            {
                _tuneBandwidth = value;

                OnPropertyChanged(nameof(TuneBandwidth));
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

        private async Task AbortTune()
        {
            _tuningAborted = true;
        }

        private async Task Tune()
        {
            _loggingService.Info($"Tuning");

            State = TuneState.TuningInProgress;

            if (ManualTuning)
            {
                Status = $"Searching channels on freq {TuneFrequency}...";                
            } else
            {
                Status = $"Automatic tuning ...";
            }

            TunedChannels.Clear();            

            OnPropertyChanged(nameof(AutomaticTuningLabel));
            OnPropertyChanged(nameof(AutomaticTuningProgress));

            _tuningAborted = false;

            try
            {
                if (ManualTuning)
                {
                    long freq = Convert.ToInt64(TuneFrequency) * 1000000;
                    long bandWidth = TuneBandwidth * 1000000;

                    await Tune(freq, bandWidth);
                } else
                {
                    await AutomaticTune();
                }

                Status = $"Tune finished. Total tuned channels: {TunedChannels.Count}";
                State = TuneState.TuneFinishedOK;                
            }
            catch (Exception ex)
            {                
                Status = $"Tune finished with error. Total tuned channels: {TunedChannels.Count}";
                State = TuneState.TuneFailed;
            }
            finally
            {

                OnPropertyChanged(nameof(TuningFinished));
                OnPropertyChanged(nameof(TunedChannels));
                OnPropertyChanged(nameof(AddChannelsVisible));

                OnPropertyChanged(nameof(AutomaticTuningLabel));
                OnPropertyChanged(nameof(AutomaticTuningProgress));
            }
        }

        private async Task AutomaticTune()
        {
            await Task.Run( async () =>
            {
                _loggingService.Info("Starting automatic tuning");
              
                for (var i = AutomaticTuningFirstChannel; i <= AutomaticTuningLastChannel; i++)
                {
                    AutomaticTuningActualChannel = i;
                    var freqMHz = (474 + 8 * (AutomaticTuningActualChannel - 21));
                    var freq = freqMHz * 1000000;

                    if (_tuningAborted)
                    {
                        return;
                    }

                    OnPropertyChanged(nameof(AutomaticTuningLabel));
                    OnPropertyChanged(nameof(AutomaticTuningProgress));

                    await Tune(freq, TuneBandwidth * 1000000);
                    //System.Threading.Thread.Sleep(200);
                }
        
            });           
        }

        private async Task Tune(long freq, long bandWidth)
        {
            try
            {
                for (var dvbtTypeIndex = 0; dvbtTypeIndex <= 1; dvbtTypeIndex++)
                {
                    if (!DVBTTuning && dvbtTypeIndex == 0)
                        continue;
                    if (!DVBT2Tuning && dvbtTypeIndex == 1)
                        continue;
                    
                    var dvbtTypeAsString = dvbtTypeIndex == 0 ? "DVBT" : "DVTB2";
                    Status = $"Tuning {freq / 1000000} Mhz ({dvbtTypeAsString}), channels found: {TunedChannels.Count}";

                    _loggingService.Debug(Status);

                    var searchMapPIDsResult = await _driver.SearchProgramMapPIDs(freq, bandWidth, dvbtTypeIndex);

                    switch (searchMapPIDsResult.Result)
                    {
                        case SearchProgramResultEnum.Error:
                            _loggingService.Debug("Search error");

                            SignalStrengthProgress = 0;

                            break;
                        case SearchProgramResultEnum.NoSignal:
                            _loggingService.Debug("No signal");

                            SignalStrengthProgress = 0;

                            break;
                        case SearchProgramResultEnum.NoProgramFound:
                            _loggingService.Debug("No program found");

                            SignalStrengthProgress = searchMapPIDsResult.SingalPercentStrength / 100.0;

                            break;
                        case SearchProgramResultEnum.OK:

                            SignalStrengthProgress = searchMapPIDsResult.SingalPercentStrength / 100.0;

                            var mapPIDs = new List<long>();
                            foreach (var sd in searchMapPIDsResult.ServiceDescriptors)
                            {
                                mapPIDs.Add(sd.Value);
                            }
                            _loggingService.Debug($"Program MAP PIDs found: {String.Join(",", mapPIDs)}");
                            break;
                    }

                    if (searchMapPIDsResult.Result != SearchProgramResultEnum.OK)
                    {
                        continue;
                    }

                    if (_tuningAborted)
                    {
                        _loggingService.Debug($"Tuning aborted");
                        return;
                    }

                    // searching PIDs

                    var alreadySavedChannelsCount = (await _channelService.LoadChannels()).Count;

                    foreach (var sDescriptor in searchMapPIDsResult.ServiceDescriptors)
                    {
                        _loggingService.Debug($"Searching Map PID {sDescriptor.Value}");

                        var searchPIDsResult = await _driver.SearchProgramPIDs(Convert.ToInt32(sDescriptor.Value));

                        switch (searchPIDsResult.Result)
                        {
                            case SearchProgramResultEnum.Error:
                                _loggingService.Debug($"Error scanning Map PID {sDescriptor.Value}");
                                break;
                            case SearchProgramResultEnum.NoSignal:
                                _loggingService.Debug("No signal");
                                break;
                            case SearchProgramResultEnum.NoProgramFound:
                                _loggingService.Debug("No program found");
                                break;
                            case SearchProgramResultEnum.OK:
                                var pids = string.Join(",", searchPIDsResult.PIDs);

                                var ch = new DVBTChannel();
                                ch.PIDs = pids;
                                ch.ProgramMapPID = sDescriptor.Value;
                                ch.Name = sDescriptor.Key.ServiceName;
                                ch.ProviderName = sDescriptor.Key.ProviderName;
                                ch.Frequency = freq;
                                ch.Bandwdith = bandWidth;
                                ch.Number = alreadySavedChannelsCount + TunedChannels.Count + 1;
                                ch.DVBTType = dvbtTypeIndex;

                                if (sDescriptor.Key.ServisType == 1 ||
                                    sDescriptor.Key.ServisType == 2)
                                {
                                    ch.ServiceType = (DVBTServiceType)sDescriptor.Key.ServisType;
                                } else
                                if (sDescriptor.Key.ServisType == 31)
                                {
                                    ch.ServiceType = DVBTServiceType.TV; // DVBT2 video
                                } else
                                {
                                    ch.ServiceType = DVBTServiceType.NotSupported;
                                }

                                TunedChannels.Add(ch);

                                _loggingService.Debug($"Found channel \"{sDescriptor.Key.ServiceName}\"");

                                Status = $"Tuning {freq / 1000000} Mhz ({dvbtTypeAsString}), channels found: {TunedChannels.Count}";

                                Device.BeginInvokeOnMainThread(delegate
                                {
                                    MessagingCenter.Send($"Found channel \"{sDescriptor.Key.ServiceName}\" ({ch.ServiceType})", BaseViewModel.MSG_ToastMessage);
                                });

                                break;
                        }

                        if (_tuningAborted)
                        {
                            _loggingService.Debug($"Tuning aborted");
                            return;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task FinishTune()
        {
            await Task.Run( () =>
            {
                try
                {
                    TunedChannels.Clear();

                    AutomaticTuningActualChannel = AutomaticTuningFirstChannel;

                    Status = "";
                    State = TuneState.Ready;
                }
                catch (Exception ex)
                {
                    Status = $"Error ({ex.Message})";
                }
                finally
                {
                    IsBusy = false;

                    OnPropertyChanged(nameof(AutomaticTuningLabel));
                    OnPropertyChanged(nameof(AutomaticTuningProgress));
                    OnPropertyChanged(nameof(TuningFinished));
                    OnPropertyChanged(nameof(TunedChannels));
                    OnPropertyChanged(nameof(AddChannelsVisible));
                }

            });
        }


        public async Task<int> SaveTunedChannels()
        {
            try
            {
                var c = 0;

                var channels = await _channelService.LoadChannels();
                if (channels == null) channels = new ObservableCollection<DVBTChannel>();

                foreach (var ch in TunedChannels)
                {
                    if (!BaseViewModel.ChannelExists(channels, ch.Frequency, ch.Name, ch.ProgramMapPID))
                    {
                        c++;
                        channels.Add(ch);
                    }
                }

                await _channelService.SaveChannels(channels);

                TunedChannels.Clear();

                await FinishTune();

                return c;
            }
            catch (Exception ex)
            {
                Status = $"Error ({ex.Message})";
                return 0;
            }
            finally
            {
                IsBusy = false;

                OnPropertyChanged(nameof(TuningFinished));
                OnPropertyChanged(nameof(TunedChannels));
                OnPropertyChanged(nameof(AddChannelsVisible));
            }
        }

    }
}
