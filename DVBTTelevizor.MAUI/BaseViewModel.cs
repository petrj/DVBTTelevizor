using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI
{
    public class BaseViewModel : BaseNotifableObject
    {
        protected ILoggingService _loggingService;
        protected IDriverConnector _driver;
        protected string _publicDirectory;
        protected ITVCConfiguration _configuration;
        protected IDialogService _dialogService;

        public BaseViewModel(ILoggingService loggingService,
            IDriverConnector driver,
            ITVCConfiguration tvConfiguration,
            IDialogService dialogService,
            IPublicDirectoryProvider publicDirectoryProvider)
        {
            _loggingService = loggingService;
            _driver = driver;
            _publicDirectory = publicDirectoryProvider.GetPublicDirectoryPath();
            _configuration = tvConfiguration;
            _dialogService = dialogService;
        }
    }
}
