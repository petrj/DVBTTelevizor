﻿using DVBTTelevizor.Models;
using LoggerService;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class TunePageViewModel : TuneViewModel
    {
        private bool _tuningAborted = false;

        private bool _DVBTTuning = true;
        private bool _DVBT2Tuning = true;

        ChannelService _channelService;

        private TuneState _tuneState = TuneState.Ready;

        private ObservableCollection<DVBTChannel> _channels = null;

        private long _actualTunningFreqKHz = -1;
        private long _actualTuningDVBTType = -1;

        private double _signalStrengthProgress = 0;

        public Command TuneCommand { get; set; }
        public Command AbortTuneCommand { get; set; }
        public Command FinishTunedCommand { get; set; }

        public enum TuneState
        {
            Ready = 0,
            TuningInProgress = 1,
            TuneFinishedOK = 2,
            TuneFailed = 3
        }

        public TunePageViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
         : base(loggingService, dialogService, driver, config, channelService)
        {
            _channelService = channelService;

            TuneCommand = new Command(async () => await Tune());
            AbortTuneCommand = new Command(async () => await AbortTune());
            FinishTunedCommand = new Command(async () => await FinishTune());
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

                var perc = (_actualTunningFreqKHz - AutoTuningFrequencyToKHz) / onePerc;
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
                OnPropertyChanged(nameof(AbortedButtonVisible));
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
                OnPropertyChanged(nameof(AbortedButtonVisible));
            }
        }

        public bool AbortedButtonVisible
        {
            get
            {
                return TuningInProgress && !TuningAborted;
            }
        }

        private async Task AbortTune()
        {
            TuningAborted = true;
            MessagingCenter.Send("FinishButton", BaseViewModel.MSG_UpdateTunePageFocus);
        }

        private async Task Tune()
        {
            _loggingService.Info($"Tuning");

            MessagingCenter.Send("AbortButton", BaseViewModel.MSG_UpdateTunePageFocus);

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
        }

        private async Task AutomaticTune()
        {
            await Task.Run(async () =>
           {
               _loggingService.Info("Starting automatic tuning");

               for (var dvbtTypeIndex = 0; dvbtTypeIndex <= 1; dvbtTypeIndex++)
               {
                   if (!DVBTTuning && dvbtTypeIndex == 0)
                       continue;
                   if (!DVBT2Tuning && dvbtTypeIndex == 1)
                       continue;

                   _actualTuningDVBTType = dvbtTypeIndex;

                   // TODO
                   /*
                   for (var i = AutomaticTuningFirstChannel; i <= AutomaticTuningLastChannel; i++)
                   {

                       var freqMHz = (474 + 8 * (_actualTunningChannel - 21));
                       var freq = freqMHz * 1000000;

                       _actualTunningFreqKHz = 0;

                       if (TuningAborted)
                       {
                           return;
                       }

                       OnPropertyChanged(nameof(TuningLabel));
                       OnPropertyChanged(nameof(AutomaticTuningProgress));

                       await Tune(freq, TuneBandwidth * 1000000, dvbtTypeIndex);
                   }
                   */
               }

           });
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

        public async Task FinishTune()
        {
            await Task.Run(() =>
           {
               try
               {
                   TunedChannels.Clear();

                   _actualTuningDVBTType = -1;
                   _actualTunningFreqKHz = -1;

                   State = TuneState.Ready;

                   MessagingCenter.Send(ManualTuning ? "ManualTuning" : "AutoTuning", BaseViewModel.MSG_UpdateTunePageFocus);
                   MessagingCenter.Send(String.Empty, BaseViewModel.MSG_CloseActualPage);
               }
               catch (Exception ex)
               {
                   _loggingService.Error(ex);
               }
               finally
               {
                   OnPropertyChanged(nameof(TuningLabel));
                   OnPropertyChanged(nameof(AutomaticTuningProgress));
                   OnPropertyChanged(nameof(TuningFinished));
                   OnPropertyChanged(nameof(TunedChannels));
                   OnPropertyChanged(nameof(AddChannelsVisible));
               }
           });
        }


    }
}
