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
        private string _tuneFrequency = "730";
        private long _tuneBandwidth = 8;

        private bool _DVBTTuning = true;
        private bool _DVBT2Tuning = true;

        private TuneState _tuneState = TuneState.Ready;

        public ObservableCollection<DVBTChannel> TunedChannels { get; set;  } = new ObservableCollection<DVBTChannel>();

        public Command TuneCommand { get; set; }
        public Command AbortTuneCommand { get; set; }
        public Command SaveTunedChannelsCommand { get; set; }
        public Command FinishTunedCommand { get; set; }

        public enum TuneState
        {
            Ready = 0,
            TuningInProgress = 1,
            TuneFinished = 2
        }

        public TunePageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {
            TuneCommand = new Command(async () => await Tune());
            AbortTuneCommand = new Command(async () => await AbortTune());
            SaveTunedChannelsCommand = new Command(async () => await SaveTunedChannels());
            FinishTunedCommand = new Command(async () => await FinishTune());

        }



        public bool TuneReady
        {
            get
            {
                return State == TuneState.Ready;
            }
        }

        public bool TuneOptionsVisible
        {
            get
            {
                return ManualTuning && State == TuneState.Ready;
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
                OnPropertyChanged(nameof(TuneOptionsVisible));
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

        public bool AddChannelsVisible
        {
            get
            {
                return State == TuneState.TuneFinished && TunedChannels.Count > 0;
            }
        }

        public bool TuningFinished
        {
            get
            {
                return State == TuneState.TuneFinished;
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

                OnPropertyChanged(nameof(TuneFrequency));
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
                _DVBTTuning = value;

                OnPropertyChanged(nameof(DVBT2Tuning));
            }
        }

        private async Task AbortTune()
        {
            _tuningAborted = true;
        }

        private async Task Tune()
        {
            Status = $"Searching channels on freq {TuneFrequency}...";

            TunedChannels.Clear();

            State = TuneState.TuningInProgress;

            _tuningAborted = false;

            try
            {
                long freq = Convert.ToInt64(TuneFrequency) * 1000000;
                long bandWidth = TuneBandwidth * 1000000;

                await Tune(freq, bandWidth);

                Status = $"Tuning finished";
            }
            catch (Exception ex)
            {
                Status = $"Tune error ({ex.Message})";
            }
            finally
            {
                State = TuneState.TuneFinished;
                OnPropertyChanged(nameof(TunedChannels));
                OnPropertyChanged(nameof(AddChannelsVisible));
            }
        }

        private async Task Tune(long freq, long bandWidth)
        {
            /* //debug
            await Task.Run(() =>
            {
                _tuningAborted = false;
                for (var i = 0; i < 15; i++)
                {
                    System.Threading.Thread.Sleep(1000);

                    if (_tuningAborted)
                        break;

                    var ch = new DVBTChannel();
                    ch.PIDs = "0,16,17";
                    ch.ProgramMapPID = 0;
                    ch.Name = $"Test {i}";
                    ch.ProviderName = "Mux  1";
                    ch.Frequency = 730000000;
                    ch.Bandwdith = 8000000;
                    ch.Number = i+1;
                    ch.DVBTType = 0;

                    TunedChannels.Add(ch);
                }
            });
            */

            try
            {
                for (var dvbtTypeIndex = 0; dvbtTypeIndex <= 1; dvbtTypeIndex++)
                {
                    if (!DVBTTuning && dvbtTypeIndex == 0)
                        continue;
                    if (!DVBT2Tuning && dvbtTypeIndex == 1)
                        continue;

                    var tuneRes = await _driver.Tune(freq, bandWidth, dvbtTypeIndex);

                    Status = $"Tuning freq. {TuneFrequency} Mhz (type {dvbtTypeIndex}) ...";

                    var searchMapPIDsResult = await _driver.SearchProgramMapPIDs(freq, bandWidth, dvbtTypeIndex);

                    switch (searchMapPIDsResult.Result)
                    {
                        case SearchProgramResultEnum.Error:
                            Status = "Search error";
                            break;
                        case SearchProgramResultEnum.NoSignal:
                            Status = "No signal";
                            break;
                        case SearchProgramResultEnum.NoProgramFound:
                            Status = "No program found";
                            break;
                        case SearchProgramResultEnum.OK:
                            var mapPIDs = new List<long>();
                            foreach (var sd in searchMapPIDsResult.ServiceDescriptors)
                            {
                                mapPIDs.Add(sd.Value);
                            }
                            Status = $"Program MAP PIDs found: {String.Join(",", mapPIDs)}";
                            break;
                    }

                    if (searchMapPIDsResult.Result != SearchProgramResultEnum.OK)
                    {
                        continue;
                    }

                    if (_tuningAborted)
                    {
                        Status = $"Tuning aborted";
                        return;
                    }

                    // searching PIDs

                    foreach (var sDescriptor in searchMapPIDsResult.ServiceDescriptors)
                    {
                        Status = $"Searching Map PID {sDescriptor.Value}";

                        var searchPIDsResult = await _driver.SearchProgramPIDs(Convert.ToInt32(sDescriptor.Value));

                        switch (searchPIDsResult.Result)
                        {
                            case SearchProgramResultEnum.Error:
                                Status = $"Error scanning Map PID {sDescriptor.Value}";
                                break;
                            case SearchProgramResultEnum.NoSignal:
                                Status = "No signal";
                                break;
                            case SearchProgramResultEnum.NoProgramFound:
                                Status = "No program found";
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
                                ch.Number = 0;
                                ch.DVBTType = dvbtTypeIndex;

                                TunedChannels.Add(ch);

                                Status = $"Found channel \"{sDescriptor.Key.ServiceName}\"";

                                break;
                        }

                        if (_tuningAborted)
                        {
                            Status = $"Tuning aborted";
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
            try
            {
                TunedChannels.Clear();
                State = TuneState.Ready;
            }
            catch (Exception ex)
            {
                Status = $"Error ({ex.Message})";
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(TunedChannels));
                OnPropertyChanged(nameof(AddChannelsVisible));
            }
        }


        public async Task<int> SaveTunedChannels()
        {
            try
            {
                var c = 0;

                var channelService = new JSONChannelsService(_loggingService, _config);

                var channels = await channelService.LoadChannels();
                if (channels == null) channels = new ObservableCollection<DVBTChannel>();

                foreach (var ch in TunedChannels)
                {
                    if (!BaseViewModel.ChannelExists(channels, ch.Frequency, ch.Name, ch.ProgramMapPID))
                    {
                        c++;
                        channels.Add(ch);
                    }
                }

                await channelService.SaveChannels(channels);

                TunedChannels.Clear();

                State = TuneState.Ready;

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
                OnPropertyChanged(nameof(TunedChannels));
                OnPropertyChanged(nameof(AddChannelsVisible));
            }
        }

    }
}
