using Android.Locations;
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

        private bool _hasSignal;
        private bool _hasCarrier;
        private bool _hasSynced;
        private bool _hasLocked;
        private long _snr;

        private TuneStateEnum _tuneState = TuneStateEnum.TuningInProgress;
        private double _signalStrengthProgress = 0;
        protected IDVBTDriverManager _driver;

        private BackgroundWorker _signalStrengthBackgroundWorker = null;

        public FindSignalViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
         : base(loggingService, dialogService, driver, config)
        {
            _driver = driver;

            _driver.StatusChanged += _driver_StatusChanged;

            _signalStrengthBackgroundWorker = new BackgroundWorker();
            _signalStrengthBackgroundWorker.WorkerSupportsCancellation = true;
            _signalStrengthBackgroundWorker.DoWork += SignalStrengthBackgroundWorker_DoWork;
        }

        private void _driver_StatusChanged(object sender, EventArgs e)
        {
            if (e is DVBTTelevizor.StatusChangedEventArgs statusArgs &&
                statusArgs.Status != null &&
                statusArgs.Status is DVBTStatus status)
            {
                if (status.SuccessFlag)
                {
                    SignalStrengthProgress = status.rfStrengthPercentage / 100.0;
                    HasCarrier = status.hasCarrier == 1;
                    HasLocked = status.hasLock == 1;
                    HasSignal = status.hasSignal == 1;
                    HasSynced = status.hasSync == 1;
                    SNR = status.snr;
                } else
                {
                    SignalStrengthProgress = 0;
                    HasCarrier = false;
                    HasSignal = false;
                    HasSynced = false;
                    HasLocked = false;
                    SNR = 0;
                }
            }
        }

        public enum TuneStateEnum
        {
            TuningInProgress = 1,
            TuneFinishedOK = 2,
            TuneFailed = 4
        }

        public bool HasSignal
        {
            get
            {
                return _hasSignal;
            }
            set
            {
                _hasSignal = value;

                OnPropertyChanged(nameof(HasSignal));
            }
        }

        public bool HasCarrier
        {
            get
            {
                return _hasCarrier;
            }
            set
            {
                _hasCarrier = value;

                OnPropertyChanged(nameof(HasCarrier));
            }
        }

        public long SNR
        {
            get
            {
                return _snr;
            }
            set
            {
                _snr = value;

                OnPropertyChanged(nameof(SNR));
                OnPropertyChanged(nameof(SNRLabel));
            }
        }

        public string SNRLabel
        {
            get
            {
                return _snr.ToString();
            }
        }

        public bool HasSynced
        {
            get
            {
                return _hasSynced;
            }
            set
            {
                _hasSynced = value;

                OnPropertyChanged(nameof(HasSynced));
            }
        }

        public bool HasLocked
        {
            get
            {
                return _hasLocked;
            }
            set
            {
                _hasLocked = value;

                OnPropertyChanged(nameof(HasLocked));
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
                return (_signalStrengthProgress * 100).ToString("N0") + "%";
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
                    _driver.Connected &&
                    (
                        _tuneState == TuneStateEnum.TuneFinishedOK
                    );
            }
        }

        public bool RetuneButtonVisible
        {
            get
            {
                return
                    _tuneState != TuneStateEnum.TuningInProgress;
            }
        }

        public string TuningStateTitle
        {
            get
            {
                switch (_tuneState)
                {
                    case TuneStateEnum.TuneFailed: return "Tune failed";
                    default:
                        return "";
                }
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
                OnPropertyChanged(nameof(IsTuned));
                OnPropertyChanged(nameof(RetuneButtonVisible));
                OnPropertyChanged(nameof(TuningStateTitle));
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
                var ok = true;

                var tuneRes = await _driver.Tune(FrequencyKHz * 1000, TuneBandWidthKHz * 1000, DeliverySystem);

                if (!tuneRes.SuccessFlag)
                {
                    ok = false;
                } else
                {
                    var setPIDres = await _driver.SetPIDs(new List<long>() { 0, 17, 18 });

                    ok = setPIDres.SuccessFlag;
                }

                if (ok)
                {
                    TuneState = TuneStateEnum.TuneFinishedOK;
                }
                else
                {
                    //MessagingCenter.Send($"Tune error", BaseViewModel.MSG_ToastMessage);
                    TuneState = TuneStateEnum.TuneFailed;
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

                            await _driver.GetStatus();

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
