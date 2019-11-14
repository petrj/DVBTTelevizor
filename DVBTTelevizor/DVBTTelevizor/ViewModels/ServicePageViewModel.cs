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

        public int SelectedDeliverySystemTypeIndex
        {
            get
            {
                return SelectedDeliverySystemType == null ? -1 : SelectedDeliverySystemType.Index;
            }
            set
            {
                foreach (var ds in DeliverySystemTypes )
                {
                    if (ds.Index == value)
                    {
                        SelectedDeliverySystemType = ds;
                        break;
                    }
                }
            }
        }

    }
}
