using LoggerService;
using System;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class BandWidthPageViewModel : BaseViewModel
    {
        private long _bandWidthKHz { get; set; } = 8000;

        public Command SetDefaultBandWidthCommand { get; set; }

        public BandWidthPageViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {
            SetDefaultBandWidthCommand = new Command(() => { BandWidthKHz = TuneViewModel.BandWidthDefaultKHz; });
        }

        public long BandWidthKHz
        {
            get
            {
                return _bandWidthKHz;
            }
            set
            {
                _bandWidthKHz = value;

                if (!ValidBandWidth(value))
                    return;

                OnPropertyChanged(nameof(BandWidthKHz));
                OnPropertyChanged(nameof(BandWidthMHz));
                OnPropertyChanged(nameof(BandWidthPickerIndex));
            }
        }

        public double BandWidthMHz
        {
            get
            {
                return BandWidthKHz / 1000.0;
            }
            set
            {
                if (double.IsNaN(value) || !ValidBandWidth(value*1000))
                    return;

                var freqKHz = Convert.ToInt64(value * 1000.0);

                if (BandWidthKHz != freqKHz)
                {
                    BandWidthKHz = freqKHz;
                }
            }
        }

        public int BandWidthPickerIndex
        {
            get
            {
                switch (BandWidthKHz)
                {
                    case 01700: return 1;
                    case 05000: return 2;
                    case 06000: return 3;
                    case 07000: return 4;
                    case 08000: return 5;
                    case 10000: return 6;
                    default:
                        return 0;
                }
            }
            set
            {
                long bandWidthKHz;

                switch (value)
                {
                    case 0:
                        return; // custom bandwidth
                    case 1:
                        bandWidthKHz = 1700;
                        break;
                    case 2:
                        bandWidthKHz = 5000;
                        break;
                    case 3:
                        bandWidthKHz = 6000;
                        break;
                    case 4:
                        bandWidthKHz = 7000;
                        break;
                    case 5:
                        bandWidthKHz = 8000;
                        break;
                    case 6:
                        bandWidthKHz = 10000;
                        break;
                    default:
                        return;
                }

                if (BandWidthKHz != bandWidthKHz)
                {
                    BandWidthKHz = bandWidthKHz;

                    OnPropertyChanged(nameof(BandWidthMHz));
                    OnPropertyChanged(nameof(BandWidthKHz));
                }

                OnPropertyChanged(nameof(BandWidthPickerIndex));
            }
        }

        public bool ValidBandWidth(double freqKHz)
        {
            return ValidBandWidth(Convert.ToInt64(freqKHz));
        }

        public bool ValidBandWidth(long freqKHz)
        {

            if (freqKHz < TuneViewModel.BandWidthMinKHz || freqKHz > TuneViewModel.BandWidthMaxKHz)
            {
                return false;
            }

            return true;
        }

        public bool ValidBandWidth(string freqMHz)
        {
            var freqKHz = TuneViewModel.ParseFreqMHzToKHz(freqMHz);
            if (freqKHz == -1)
            {
                return false;
            }

            return ValidBandWidth(freqKHz);
        }
    }
}
