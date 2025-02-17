using LoggerService;
using Microsoft.Maui;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class TuningSelectDVBTPageViewModel : BaseViewModel
    {
        private bool _dvbt = true;
        private bool _dvbt2 = true;

        private string? _selectedBandwidth = null;
        private Dictionary<string, int> _dict = new Dictionary<string, int>();

        public ObservableCollection<string> Bandwidths { get; set; } = new ObservableCollection<string>();

        public TuningSelectDVBTPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
            _dvbt = tvConfiguration.TuneDVBTEnabled;
            _dvbt2 = tvConfiguration.TuneDVBT2Enabled;

            _selectedBandwidth = Bandwidth.BandWidthTitle[tvConfiguration.DVBTBandwidth];
        }

        public async void FillBandwidths()
        {
            Bandwidths.Clear();
            _dict.Clear();

            foreach (var key in Bandwidth.TitleBandWidthHz.Keys)
            {
                Bandwidths.Add(key);
            }

            if (_selectedBandwidth == null)
            {
                _selectedBandwidth = "8 MHz";
            }

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                OnPropertyChanged(nameof(Bandwidths));
                OnPropertyChanged(nameof(SelectedBandwidth));
            });
        }

        public string? SelectedBandwidth
        {
            get
            {
                return _selectedBandwidth;
            }
            set
            {
                if (value == null)
                {
                    return;
                }

                _selectedBandwidth = value;

                if (Bandwidth.TitleBandWidth.ContainsKey(value))
                {
                    _configuration.DVBTBandwidth = Bandwidth.TitleBandWidth[value];
                }

                OnPropertyChanged(nameof(SelectedBandwidth));
            }
        }

        public bool DVBT
        {
            get
            {
                return _dvbt;
            }
            set
            {
                _dvbt = value;
                OnPropertyChanged(nameof(DVBT));
                OnPropertyChanged(nameof(NextVisible));
                _configuration.TuneDVBTEnabled = value;
            }
        }

        public bool DVBT2
        {
            get
            {
                return _dvbt2;
            }
            set
            {
                _dvbt2 = value;
                OnPropertyChanged(nameof(DVBT2));
                OnPropertyChanged(nameof(NextVisible));
                _configuration.TuneDVBT2Enabled = value;
            }
        }

        public bool NextVisible
        {
            get
            {
                return DVBT || DVBT2;
            }
        }
    }
}

