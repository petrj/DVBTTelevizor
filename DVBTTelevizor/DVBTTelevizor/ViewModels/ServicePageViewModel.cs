using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LoggerService;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System.Threading;
using Newtonsoft.Json;
using DVBTTelevizor.Models;

namespace DVBTTelevizor
{
    public class ServicePageViewModel : TuneViewModel
    {
        private int _tuneDVBTType = 0;

        public ObservableCollection<DVBTDeliverySystemType> DeliverySystemTypes { get; set; } = new ObservableCollection<DVBTDeliverySystemType>();

        DVBTDeliverySystemType _selectedDeliverySystemType = null;

        public Command GetVersionCommand { get; set; }

        public Command GetStatusCommand { get; set; }

        public ServicePageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {
            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_UpdateDriverState, (message) =>
            {
                UpdateDriverState();
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DVBTDriverConfigurationFailed, (message) =>
            {
                Device.BeginInvokeOnMainThread(delegate
                {
                    Status = $"Initialization failed ({message})";
                    UpdateDriverState();
                });
            });

            FillDeliverySystemTypes();

            GetVersionCommand = new Command(async () => await GetVersion());
            GetStatusCommand = new Command(async () => await GetStatus());
        }

        private async Task GetStatus()
        {
            try
            {
                _loggingService.Info($"Getting Status");

                var status = await _driver.GetStatus();

                if (!status.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }

                var s = Environment.NewLine;

                s += $"Signal :  {status.hasSignal}";
                s += Environment.NewLine;

                s += $"Sync :  {status.hasSync}";
                s += Environment.NewLine;

                s += $"Lock :  {status.hasLock}";
                s += Environment.NewLine;

                s += $"% :  {status.rfStrengthPercentage}";
                s += Environment.NewLine;

                await _dialogService.Information(s, "Status");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while getting status");
                await _dialogService.Error(ex.Message);
            }
        }

        private async Task GetVersion()
        {
            try
            {
                _loggingService.Info($"Getting version");

                var version = await _driver.GetVersion();

                if (!version.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }

                await _dialogService.Information($"Version: {version.Version.ToString()}.{version.AllRequestsLength.ToString()}", "DVBT Driver version");                
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while getting version");
                await _dialogService.Error(ex.Message);                
            }
        }

        protected void FillDeliverySystemTypes()
        {
            DeliverySystemTypes.Clear();

            DeliverySystemTypes.Add(
                new DVBTDeliverySystemType()
                {
                    Index = 0,
                    Name = "DVBT"
                });

            DeliverySystemTypes.Add(
                new DVBTDeliverySystemType()
                {
                    Index = 1,
                    Name = "DVBT2"
                });
        }

        public int TuneDVBTType
        {
            get
            {
                return _tuneDVBTType;
            }
            set
            {
                _tuneDVBTType = value;

                OnPropertyChanged(nameof(TuneDVBTType));
            }
        }

        public DVBTDeliverySystemType SelectedDeliverySystemType
        {
            get
            {
                return _selectedDeliverySystemType;
            }
            set
            {
                _selectedDeliverySystemType = value;

                OnPropertyChanged(nameof(DeliverySystemTypes));
                OnPropertyChanged(nameof(SelectedDeliverySystemType));
            }
        }     

    }
}
