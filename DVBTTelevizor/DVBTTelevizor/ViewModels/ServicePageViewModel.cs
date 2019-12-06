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
        private string _pids = "768,801,769,785,787,0,16,17,18";

        public ObservableCollection<DVBTDeliverySystemType> DeliverySystemTypes { get; set; } = new ObservableCollection<DVBTDeliverySystemType>();

        DVBTDeliverySystemType _selectedDeliverySystemType = null;

        public Command GetVersionCommand { get; set; }
        public Command GetStatusCommand { get; set; }
        public Command GetCapabilitiesCommand { get; set; }
        public Command TuneCommand { get; set; }
        public Command PlayCommand { get; set; }

        public Command StartRecordCommand { get; set; }
        public Command StopRecordCommand { get; set; }

        public Command SetPIDsCommand { get; set; }

        public ServicePageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
         : base(loggingService, dialogService, driver, config)
        {         

            FillDeliverySystemTypes();

            GetVersionCommand = new Command(async () => await GetVersion());
            GetStatusCommand = new Command(async () => await GetStatus());
            GetCapabilitiesCommand = new Command(async () => await GetCapabilities());
            TuneCommand = new Command(async () => await Tune());

            SetPIDsCommand = new Command(async () => await SetPIDs());

            PlayCommand = new Command(async () => await Play());

            StartRecordCommand = new Command(async () => await StartRecord());
            StopRecordCommand = new Command(async () => await StopRecord());
        }

        public string PIDs
        {
            get
            {
                return _pids;
            }
            set
            {
                _pids = value;

                OnPropertyChanged(nameof(PIDs));
            }
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

        private async Task StartRecord()
        {
            try
            {
                await BaseViewModel.RunWithStoragePermission(
                  async () =>
                  {
                      await _driver.StartRecording();
                  }, _dialogService);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while start recording");
                await _dialogService.Error(ex.Message);
            }
        }

        private async Task StopRecord()
        {
            try
            {
                _driver.StopRecording();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while stop recording");
                await _dialogService.Error(ex.Message);
            }
        }

        private async Task Play()
        {
            try
            {
                if (!_driver.Started)
                {
                    await _dialogService.Error($"Driver not initialized");
                    return;
                }

                _driver.StopReadStream();

                MessagingCenter.Send("", BaseViewModel.MSG_PlayStream);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while playing");
                await _dialogService.Error(ex.Message);
            }
        }

        private async Task SetPIDs()
        {
            try
            {
                var pids = new List<long>();

                foreach (var PIDAsString in PIDs.Split(','))
                {
                    pids.Add(Convert.ToInt64(PIDAsString));
                }

                var pidRes = await _driver.SetPIDs(pids);

                if (!pidRes.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }

                await _dialogService.Information($"OK", "PID Filter response");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while setting PID filter");
                await _dialogService.Error(ex.Message);
            }
        }

        private async Task Tune()
        {
            try
            {
                var freq = Convert.ToInt64(TuneFrequency) * 1000000;
                var bandWidth = Convert.ToInt64(TuneBandwidth) * 1000000;
                var type = SelectedDeliverySystemType == null ? 0 : SelectedDeliverySystemType.Index;

                _loggingService.Info($"Tuning {TuneFrequency} Mhz, {TuneBandwidth} bandwidth, type {type} ");

                var tuneRes = await _driver.Tune(freq, bandWidth, type);

                if (!tuneRes.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }

                await _dialogService.Information($"OK", "Tune response");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while tuning");
                await _dialogService.Error(ex.Message);
            }
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

        private async Task GetCapabilities()
        {
            try
            {
                _loggingService.Info($"Getting Capabilities");

                var cap = await _driver.GetCapabalities();

                if (!cap.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }

                var s = Environment.NewLine;

                s += $"VendorId:  {cap.vendorId}";
                s += Environment.NewLine;

                s += $"ProductId:  {cap.productId}";
                s += Environment.NewLine;

                s += $"Frequency step size:  {cap.frequencyStepSize}";
                s += Environment.NewLine;

                s += $"Frequency minimum:  {cap.minFrequency/1000000}";
                s += Environment.NewLine;

                s += $"Frequency maximum:  {cap.maxFrequency / 1000000}";
                s += Environment.NewLine;

                s += $"Supported DeliverySystems:";
                s += Environment.NewLine;
                s += $"  DVBT :  YES";
                s += Environment.NewLine;
                s += $"  DVBT2:  ";
                if (cap.supportedDeliverySystems % 1 == 1)
                {
                    s += $"YES";
                } else
                {
                    s += $"NO";
                }
                s += Environment.NewLine;

                await _dialogService.Information(s, "DVBT Driver capabilities");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while getting capabilities");
                await _dialogService.Error(ex.Message);
            }
        }

        private void FillDeliverySystemTypes()
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
    }
}
