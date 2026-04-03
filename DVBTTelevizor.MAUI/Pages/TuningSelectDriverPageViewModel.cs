using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using DVBTTelevizor.TV;
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

        public ICommand CommandFM { get; set; }
        public ICommand CommandDAB { get; set; }

        public TuningSelectDriverPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            CommandDVBT = new Command(() =>
            {
                _loggingService.Info($"TuningSelectDriverPageViewModel: CommandDVBT executed");
                WeakReferenceMessenger.Default.Send(new ShowDriverPageMessage(AppDriverTypeEnum.DVBT));
            });

            CommandFM = new Command(() =>
            {
                _loggingService.Info($"TuningSelectDriverPageViewModel: CommandFM executed");
                WeakReferenceMessenger.Default.Send(new ShowDriverPageMessage(AppDriverTypeEnum.FM));
            });

            CommandDAB = new Command(() =>
            {
                _loggingService.Info($"TuningSelectDriverPageViewModel: CommandDAB executed");
                WeakReferenceMessenger.Default.Send(new ShowDriverPageMessage(AppDriverTypeEnum.DAB));
            });
        }
    }
}

