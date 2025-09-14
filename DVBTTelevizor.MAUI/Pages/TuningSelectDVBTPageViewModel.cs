using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
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
        private string? _selectedBandwidth = null;
        private Dictionary<string, int> _dict = new Dictionary<string, int>();

        public bool Initializing { get; set; } = true;

        public ObservableCollection<string> Bandwidths { get; set; } = new ObservableCollection<string>();

        public TuningSettings Settings { get; set; }

        public TuningSelectDVBTPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            Settings = new TuningSettings(loggingService);

            if (!Bandwidth.BandWidthHzTitle.ContainsKey(tvConfiguration.DVBTBandwidthKHz * 1000))
            {
                _selectedBandwidth = "8 MHz";
            }
            else
            {
                _selectedBandwidth = Bandwidth.BandWidthHzTitle[tvConfiguration.DVBTBandwidthKHz * 1000];
            }
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
                Settings.BandwidthKHz = SelectedBandwidthKHz;

                OnPropertyChanged(nameof(SelectedBandwidth));
                OnPropertyChanged(nameof(SelectedBandwidthKHz));
            }
        }

        public long SelectedBandwidthKHz
        {
            get
            {
                if (_selectedBandwidth == null)
                {
                    return 8000;
                }

                if (Bandwidth.TitleBandWidth.ContainsKey(_selectedBandwidth))
                {
                    return Bandwidth.TitleBandWidthHz[_selectedBandwidth] / 1000;
                }

                return 8000;
            }
            set
            {

                if (Bandwidth.BandWidthHzTitle.ContainsKey(value*1000))
                {
                    _selectedBandwidth = Bandwidth.BandWidthHzTitle[value * 1000];
                } else
                {
                    _selectedBandwidth = "8 MHz";
                }

                OnPropertyChanged(nameof(SelectedBandwidth));
            }
        }

        public bool DVBT
        {
            get
            {
                return Settings.DVBT;
            }
            set
            {
                Settings.DVBT = value;

                Settings.TuneDVBTPreferred = Settings.DVBT;

                if (Settings.TuningMode == TuneModeEnum.Frequency && Settings.DVBT == Settings.DVBT2)
                {
                    DVBT2 = !Settings.DVBT;
                }

                OnPropertyChanged(nameof(DVBT));
                OnPropertyChanged(nameof(DVBT2));
                OnPropertyChanged(nameof(NextVisible));
            }
        }

        public bool DVBT2
        {
            get
            {
                return Settings.DVBT2;
            }
            set
            {
                Settings.DVBT2 = value;

                Settings.TuneDVBTPreferred = !Settings.DVBT2;

                if (Settings.TuningMode == TuneModeEnum.Frequency && Settings.DVBT == Settings.DVBT2)
                {
                    DVBT = !Settings.DVBT2;
                }

                OnPropertyChanged(nameof(DVBT));
                OnPropertyChanged(nameof(DVBT2));
                OnPropertyChanged(nameof(NextVisible));
            }
        }

        public bool NextVisible
        {
            get
            {
                return DVBT || DVBT2;
            }
        }

        public void Update()
        {
            DVBT = _configuration.TuneDVBTEnabled;
            DVBT2 = _configuration.TuneDVBT2Enabled;

            if (Settings.TuningMode == TuneModeEnum.Frequency)
            {
                DVBT = Settings.TuneDVBTPreferred;
            }

            if (Bandwidth.BandWidthHzTitle.ContainsKey(_configuration.DVBTBandwidthKHz * 1000))
            {
                SelectedBandwidth = Bandwidth.BandWidthHzTitle[_configuration.DVBTBandwidthKHz * 1000];
            }
        }
    }
}

