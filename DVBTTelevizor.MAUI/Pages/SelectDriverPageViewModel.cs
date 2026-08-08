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
    public class SelectDriverPageViewModel : BaseViewModel
    {
        public ICommand CommandDVBT { get; set; }

        public ICommand CommandFM { get; set; }
        public ICommand CommandDAB { get; set; }

        private IDriverConnector _driver;

        public SelectDriverPageViewModel(ILoggingService loggingService, IDriverConnector driver, ITVConfiguration tvConfiguration, IPublicDirectoryProvider publicDirectoryProvider)
          : base(loggingService, driver, tvConfiguration, publicDirectoryProvider)
        {
            _driver = driver;

            WeakReferenceMessenger.Default.Register<DriverChangedMessage>(this, (r, m) =>
            {
                _driver = m.Value;
                NotifyChange();
            });

            CommandDVBT = new Command(() =>
            {
                _loggingService.Info($"TuningSelectDriverPageViewModel: CommandDVBT executed");
                WeakReferenceMessenger.Default.Send(new ShowSelectDriverDriverPageMessage(AppDriverTypeEnum.DVBT));
            });

            CommandFM = new Command(() =>
            {
                _loggingService.Info($"TuningSelectDriverPageViewModel: CommandFM executed");
                WeakReferenceMessenger.Default.Send(new ShowSelectDriverDriverPageMessage(AppDriverTypeEnum.FM));
            });

            CommandDAB = new Command(() =>
            {
                _loggingService.Info($"TuningSelectDriverPageViewModel: CommandDAB executed");
                WeakReferenceMessenger.Default.Send(new ShowSelectDriverDriverPageMessage(AppDriverTypeEnum.DAB));
            });
        }

        public string DVBTDriverImage
        {
            get
            {
                return (_driver != null) && (_driver.DriverType == AppDriverTypeEnum.DVBT) ? "dvbtconnected.png" : "dvbt.png";
            }
        }

        public string FMDriverImage
        {
            get
            {
                return (_driver != null) && (_driver.DriverType == AppDriverTypeEnum.FM) ? "fmconnected.png" : "fm.png";
            }
        }

        public string DABDriverImage
        {
            get
            {
                return (_driver != null) && (_driver.DriverType == AppDriverTypeEnum.DAB) ? "dabconnected.png" : "dab.png";
            }
        }


        public async void NotifyChange()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                OnPropertyChanged(nameof(DVBTDriverImage));
                OnPropertyChanged(nameof(FMDriverImage));
                OnPropertyChanged(nameof(DABDriverImage));
            });
        }
    }
}

