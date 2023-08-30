using Android.Preferences;
using LoggerService;
using MPEGTS;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.UI.Views;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class FindSignalViewModel : BaseViewModel
    {
        private long _frequencyKHz;
        private long _tuneBandWidthKHz;
        private int _deliverySystem;

        private TuneStateEnum _tuneState = TuneStateEnum.TuningInProgress;
        private double _signalStrengthProgress = 0;
        protected IDVBTDriverManager _driver;

        private BackgroundWorker _signalStrengthBackgroundWorker = null;

        public FindSignalViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
         : base(loggingService, dialogService, driver, config)
        {
            _driver = driver;

            _signalStrengthBackgroundWorker = new BackgroundWorker();
            _signalStrengthBackgroundWorker.WorkerSupportsCancellation = true;
            _signalStrengthBackgroundWorker.DoWork += SignalStrengthBackgroundWorker_DoWork;
        }

        public enum TuneStateEnum
        {
            TuningInProgress = 1,
            TuneFinishedOK = 2,
            TuneFinishedNoSignal = 3,
            TuneFailed = 4
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
                OnPropertyChanged(nameof(FrequencyWholePartMHz));
                OnPropertyChanged(nameof(FrequencyDecimalPartMHzCaption));
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
            }
        }

        public int DeliverySystem
        {
            get
            {
                return _deliverySystem;
            }
            set
            {
                _deliverySystem = value;

                OnPropertyChanged(nameof(DeliverySystem));
                OnPropertyChanged(nameof(DeliverySystemCaption));
            }
        }

        public string DeliverySystemCaption
        {
            get
            {
                return DeliverySystem == 0 ? "DVBT" : "DVBT2";
            }
        }

        public bool IsTuning
        {
            get
            {
                return _tuneState == TuneStateEnum.TuningInProgress;
            }
        }

        public bool IsTuned
        {
            get
            {
                return
                    _tuneState == TuneStateEnum.TuneFinishedOK ||
                    _tuneState == TuneStateEnum.TuneFinishedNoSignal;
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

        public TuneStateEnum TuneState
        {
            get
            {
                return _tuneState;
            }
            set
            {
                _tuneState = value;

                OnPropertyChanged(nameof(TuneState));
                OnPropertyChanged(nameof(IsTuning));
            }
        }

        public async Task Tune()
        {
            _loggingService.Info($"Tuning {FrequencyKHz} KHz");

            TuneState = TuneStateEnum.TuningInProgress;

            if (!_driver.Connected)
            {
                MessagingCenter.Send($"Device not connected", BaseViewModel.MSG_ToastMessage);
                TuneState = TuneStateEnum.TuneFailed;
                return;
            }

            try
            {
                // this fix the "MUX switching no driver data error"
                await _driver.Tune(0, TuneBandWidthKHz * 1000, DeliverySystem);

                var status = await _driver.TuneEnhanced(FrequencyKHz * 1000, TuneBandWidthKHz * 1000, DeliverySystem, new List<long>() { 0, 17 }, false);

                switch (status.Result)
                {
                    case SearchProgramResultEnum.NoSignal:
                        MessagingCenter.Send($"No signal", BaseViewModel.MSG_ToastMessage);
                        TuneState = TuneStateEnum.TuneFinishedNoSignal;
                        break;
                    case SearchProgramResultEnum.Error:
                        MessagingCenter.Send($"Tune error", BaseViewModel.MSG_ToastMessage);
                        TuneState = TuneStateEnum.TuneFailed;
                        break;
                    case SearchProgramResultEnum.OK:
                        TuneState = TuneStateEnum.TuneFinishedOK;
                        break;
                }
            }
            catch (Exception ex)
            {
                MessagingCenter.Send($"Tune error", BaseViewModel.MSG_ToastMessage);
                TuneState = TuneStateEnum.TuneFailed;
            }
        }

        public async Task Start()
        {
            _signalStrengthBackgroundWorker.RunWorkerAsync();
        }

        public async Task Stop()
        {
            _signalStrengthBackgroundWorker.CancelAsync();

            if (_driver.Connected)
            {
                await _driver.SetPIDs(new List<long>() { });
            }
        }

        private void SignalStrengthBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            _loggingService.Info("Starting SignalStrengthBackgroundWorker_DoWork");

            while (!_signalStrengthBackgroundWorker.CancellationPending)
            {
                try
                {
                    if (_driver.Connected && IsTuned)
                    {
                        Task.Run(async () =>
                        {
                            _loggingService.Debug("SignalStrengthBackgroundWorker_DoWork: calling GetStatus");

                            var status = await _driver.GetStatus();

                            _loggingService.Debug("SignalStrengthBackgroundWorker_DoWork: calling GetStatus");

                            if (status.SuccessFlag)
                            {
                                SignalStrengthProgress = status.rfStrengthPercentage / 100.0;
                            }
                        }).Wait();
                    }
                    else
                    {
                        SignalStrengthProgress = 0;
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
                finally
                {

                }

                System.Threading.Thread.Sleep(1000);
            }

            _loggingService.Info("SignalStrengthBackgroundWorker_DoWork finished");
        }

    }
}
