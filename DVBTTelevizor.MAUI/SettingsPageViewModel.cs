using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class SettingsPageViewModel : BaseViewModel
    {
        public SettingsPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVCConfiguration tvConfiguration, IDialogService dialogService, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, dialogService, publicDirectoryProvider)
        {

        }

        public int AppFontSizeIndex
        {
            get
            {
                return (int)_configuration.AppFontSize;
            }
            set
            {
                _configuration.AppFontSize = (AppFontSizeEnum)value;

                OnPropertyChanged(nameof(AppFontSizeIndex));
                NotifyFontSizeChange();
            }
        }

    }
}
