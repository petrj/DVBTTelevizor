﻿using Xamarin.Forms;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LoggerService;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using DVBTTelevizor.Models;

namespace DVBTTelevizor
{
    public class ServicePageViewModel : TuneViewModel
    {
        private int _tuneDVBTType = 0;
        private string _pids = "0,16,17,18";
        private bool _scaningInProgress = false;

        public ObservableCollection<DVBTDeliverySystemType> DeliverySystemTypes { get; set; } = new ObservableCollection<DVBTDeliverySystemType>();

        DVBTDeliverySystemType _selectedDeliverySystemType = null;

        public Command GetVersionCommand { get; set; }
        public Command GetStatusCommand { get; set; }
        public Command GetCapabilitiesCommand { get; set; }
        public Command TuneCommand { get; set; }
        public Command PlayCommand { get; set; }

        public Command ScanPSICommand { get; set; }

        public Command ScanEITCommand { get; set; }

        public Command StartRecordCommand { get; set; }
        public Command StopRecordCommand { get; set; }

        public Command SetPIDsCommand { get; set; }

        public ServicePageViewModel(ILoggingService loggingService, IDialogService dialogService, IDVBTDriverManager driver, DVBTTelevizorConfiguration config, ChannelService channelService)
         : base(loggingService, dialogService, driver, config, channelService)
        {

            FillDeliverySystemTypes();

            GetVersionCommand = new Command(async () => await GetVersion());
            GetStatusCommand = new Command(async () => await GetStatus());
            GetCapabilitiesCommand = new Command(async () => await GetCapabilities());
            TuneCommand = new Command(async () => await Tune());

            SetPIDsCommand = new Command(async () => await SetPIDs());

            PlayCommand = new Command(async () => await Play());

            ScanPSICommand = new Command(async () => await ScanPSI());
            ScanEITCommand = new Command(async () => await ScanEIT());

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

        public bool ScaningInProgress
        {
            get
            {
                return _scaningInProgress;
            }
            set
            {
                _scaningInProgress = value;

                OnPropertyChanged(nameof(ScaningInProgress));
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
                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                await _driver.StartRecording();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while start recording");
                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task StopRecord()
        {
            try
            {
                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                _driver.StopRecording();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while stop recording");
                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task Play()
        {
            try
            {
                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                _driver.StopReadStream();

                await Task.Delay(500);

                MessagingCenter.Send(new PlayStreamInfo(), BaseViewModel.MSG_PlayStream);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while playing");
                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task SetPIDs()
        {
            try
            {
                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

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

                MessagingCenter.Send("PID Filter applied", BaseViewModel.MSG_ToastMessage);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while setting PID filter");
                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task Tune()
        {
            try
            {
                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                var freq = Convert.ToInt64(TuningFrequencyKHz) * 1000;
                var bandWidth = Convert.ToInt64(TuneBandWidthKHz) * 1000;
                var type = SelectedDeliverySystemType == null ? 0 : SelectedDeliverySystemType.Index;

                _loggingService.Info($"Tuning {TuningFrequencyKHz} KHz, {TuneBandWidthKHz} bandwidth, type {type} ");

                var tuneRes = await _driver.Tune(freq, bandWidth, type);

                if (!tuneRes.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }

                MessagingCenter.Send($"Frequency {TuningFrequencyKHz} successfully tuned", BaseViewModel.MSG_ToastMessage);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while tuning");

                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task GetStatus()
        {
            try
            {
                _loggingService.Info($"Getting Status");

                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                var status = await _driver.GetStatus();

                if (!status.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }

                var s = $"Signal: {status.hasSignal}, sync: { status.hasSync}, lock : {status.hasLock}, stregth: {status.rfStrengthPercentage}%";

                MessagingCenter.Send(s, BaseViewModel.MSG_ToastMessage);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while getting status");

                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task GetVersion()
        {
            try
            {
                _loggingService.Info($"Getting version");

                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                var version = await _driver.GetVersion();

                if (!version.SuccessFlag)
                {
                    throw new Exception("Response not success");
                }

                var s = $"Version: {version.Version.ToString()}.{version.AllRequestsLength.ToString()}";
                MessagingCenter.Send(s, BaseViewModel.MSG_ToastMessage);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while getting version");

                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ScanEIT()
        {
            try
            {
                _loggingService.Info($"Scanning EIT");

                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                ScaningInProgress = true;

                var res = await _driver.ScanEPG(Convert.ToInt64(TuningFrequencyKHz) * 1000, 5000);

                ScaningInProgress = false;

                if (res.OK)
                {
                    var txt = String.Empty;
                    foreach (var kvp in _driver.GetEITManager(Convert.ToInt64(TuningFrequencyKHz) * 1000).CurrentEvents)
                    {
                        txt += $"Service: {kvp.Key}  Event: {kvp.Value.TextValue} {Environment.NewLine}";
                    }

                    await _dialogService.Information(txt);

                }
                else
                {
                    MessagingCenter.Send("Scan error", BaseViewModel.MSG_ToastMessage);
                }

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while scanning PSI");

                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
            finally
            {
                ScaningInProgress = false;
            }
        }

        private async Task ScanPSI()
        {
            try
            {
                _loggingService.Info($"Scanning PSI");

                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

                ScaningInProgress = true;

                var searchMapPIDsResult = await _driver.SearchProgramMapPIDs();

                switch (searchMapPIDsResult.Result)
                {
                    case SearchProgramResultEnum.Error:
                        MessagingCenter.Send("Search error", BaseViewModel.MSG_ToastMessage);
                        return;
                    case SearchProgramResultEnum.NoSignal:
                        MessagingCenter.Send("No signal", BaseViewModel.MSG_ToastMessage);
                        return;
                    case SearchProgramResultEnum.NoProgramFound:
                        MessagingCenter.Send("No program found", BaseViewModel.MSG_ToastMessage);
                        return;
                }

                var mapPIDs = new List<long>();
                var mapPIDToName = new Dictionary<long, string>();
                foreach (var sd in searchMapPIDsResult.ServiceDescriptors)
                {
                    mapPIDs.Add(sd.Value);
                    mapPIDToName.Add(sd.Value, sd.Key.ServiceName);
                }
                _loggingService.Debug($"Program MAP PIDs found: {String.Join(",", mapPIDs)}");

                // searching PIDs

                var searchProgramPIDsResult = await _driver.SearchProgramPIDs(mapPIDs);

                ScaningInProgress = false;

                switch (searchProgramPIDsResult.Result)
                {
                    case SearchProgramResultEnum.Error:
                        MessagingCenter.Send("Error scanning Map PIDs", BaseViewModel.MSG_ToastMessage);
                        return;
                    case SearchProgramResultEnum.NoSignal:
                        MessagingCenter.Send("No signal", BaseViewModel.MSG_ToastMessage);
                        return;
                    case SearchProgramResultEnum.NoProgramFound:
                        MessagingCenter.Send("No program found", BaseViewModel.MSG_ToastMessage);
                        return;
                }

                var list = new List<string>();
                foreach (var kvp in searchProgramPIDsResult.PIDs)
                {
                    list.Add($"{mapPIDToName[kvp.Key]}: {string.Join(",", kvp.Value)}");
                }

                var res = await _dialogService.DisplayActionSheet("Select PIDs:", "Cancel", list);

                if (res != "Cancel")
                {
                    foreach (var kvp in searchProgramPIDsResult.PIDs)
                    {
                        if ($"{mapPIDToName[kvp.Key]}: {string.Join(",", kvp.Value)}" == res)
                        {
                            PIDs = "0,16,17,18," + kvp.Key + "," + string.Join(",", kvp.Value);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while scanning PSI");
                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
            }
            finally
            {
                ScaningInProgress = false;
            }
        }

        private async Task GetCapabilities()
        {
            try
            {
                _loggingService.Info($"Getting Capabilities");

                if (!_driver.Started)
                {
                    MessagingCenter.Send("Device not connected", BaseViewModel.MSG_ToastMessage);
                    return;
                }

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

                s += $"Frequency minimum:  {cap.minFrequency / 1000000}";
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
                }
                else
                {
                    s += $"NO";
                }
                s += Environment.NewLine;

                await _dialogService.Information(s, "Device capabilities");
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while getting capabilities");
                MessagingCenter.Send(ex.Message, BaseViewModel.MSG_ToastMessage);
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
