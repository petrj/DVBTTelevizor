using DVBTTelevizor.Models;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml.Internals;

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

                if (!ValidFrequency(value))
                    return;

                OnPropertyChanged(nameof(BandWidthKHz));
                OnPropertyChanged(nameof(BandWidthMHz));
                OnPropertyChanged(nameof(BandWidthPickerIndex));
            }
        }

        public string BandWidthMHz
        {
            get
            {
                return Math.Round(BandWidthKHz / 1000.0, 3).ToString();
            }
            set
            {
                if (!ValidFrequency(value))
                    return;

                var freqKHz = ParseFreqMHzToKHz(value);

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


        public bool ValidFrequency(long freqKHz)
        {

            if (freqKHz < TuneViewModel.BandWidthMinKHz || freqKHz > TuneViewModel.BandWidthMaxKHz)
            {
                return false;
            }

            return true;
        }

        public bool ValidFrequency(string freqMHz)
        {
            var freqKHz = ParseFreqMHzToKHz(freqMHz);
            if (freqKHz == -1)
            {
                return false;
            }

            return ValidFrequency(freqKHz);
        }

        public long ParseFreqMHzToKHz(string freqMHz)
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
    }
}
