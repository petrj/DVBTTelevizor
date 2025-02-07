using LoggerService;
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

        public TuningSelectDVBTPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {
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

