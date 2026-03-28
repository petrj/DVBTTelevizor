using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using LoggerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DVBTTelevizor.MAUI
{
    public class TuningSelectDriverPageViewModel : BaseViewModel
    {
        public ICommand CommandDVBT { get; set; }
        public ICommand CommandSDR { get; set; }

        public TuningSelectDriverPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            CommandDVBT = new Command(() =>
            {
                _loggingService.Info($"TuningSelectDriverPageViewModel: CommandDVBT executed");
                WeakReferenceMessenger.Default.Send(new ShowDriverPageMessage(DriverTypeEnum.AndroidDVBTDriver));
            });

            CommandSDR = new Command(() =>
            {
                _loggingService.Info($"TuningSelectDriverPageViewModel: CommandSDR executed");
                WeakReferenceMessenger.Default.Send(new ShowDriverPageMessage(DriverTypeEnum.RTLSDRDriverDAB));
            });
        }        
    }
}

