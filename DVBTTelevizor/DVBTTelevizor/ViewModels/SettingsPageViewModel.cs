using LoggerService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Android.Content;
using Plugin.InAppBilling;

namespace DVBTTelevizor
{
    public class SettingsPageViewModel : ConfigViewModel
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;

        private ChannelService _channelService;

        public Command ClearChannelsCommand { get; set; }
        public Command ClearEPGCommand { get; set; }
        public Command ExportChannelsCommand { get; set; }
        public Command ImportChannelsCommand { get; set; }
        public Command ShareChannelsCommand { get; set; }

        public Command Donate1command { get; set; }
        public Command Donate2Command { get; set; }
        public Command Donate3command { get; set; }
        public Command Donate5command { get; set; }

        public bool Donate1Visible { get; set; } = true;
        public bool Donate2Visible { get; set; } = true;
        public bool Donate3Visible { get; set; } = true;
        public bool Donate5Visible { get; set; } = true;

        public ObservableCollection<DVBTChannel> AutoPlayChannels { get; set; } = new ObservableCollection<DVBTChannel>();
        public DVBTChannel _selectedChannel = null;

        public SettingsPageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTTelevizorConfiguration config, ChannelService channelService)
            :base(config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            _channelService = channelService;

            _config = config;

            ClearChannelsCommand = new Command(async () => await ClearChannels());
            ClearEPGCommand = new Command(async () => await ClearEPG());

            ExportChannelsCommand = new Command(async () => await Export());

            ImportChannelsCommand = new Command(async () => await Import());

            ShareChannelsCommand = new Command(async  () => { await ShareLog(); });

            Donate1command = new Command(async () => { await Donate("donation.2024.1"); });
            Donate2Command = new Command(async () => { await Donate("donation.2024.2"); });
            Donate3command = new Command(async () => { await Donate("donation.2024.3"); });
            Donate5command = new Command(async () => { await Donate("donation.2024.5"); });
        }

        public void HideDonateButton(string productId)
        {
            switch (productId)
            {
                case "donation.2024.1":
                    Donate1Visible = false;
                    OnPropertyChanged(nameof(Donate1Visible));
                    break;
                case "donation.2024.2":
                    Donate2Visible = false;
                    OnPropertyChanged(nameof(Donate2Visible));
                    break;
                case "donation.2024.3":
                    Donate3Visible = false;
                    OnPropertyChanged(nameof(Donate3Visible));
                    break;
                case "donation.2024.5":
                    Donate5Visible = false;
                    OnPropertyChanged(nameof(Donate5Visible));
                    break;
            }
        }

        public async Task CheckPurchases()
        {
            try
            {
                // contacting service

                var connected = await CrossInAppBilling.Current.ConnectAsync();

                if (!connected)
                {
                    _loggingService.Info($"Connection to AppBilling service failed");
                    return;
                }

                var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);
                foreach (var purchase in purchases)
                {
                    if (purchase.State == PurchaseState.Purchased)
                    {
                        HideDonateButton(purchase.ProductId);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while checking purchases");
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }

        public async void FillAutoPlayChannels()
        {
            AutoPlayChannels.Clear();

            var noChannel = new DVBTChannel()
            {
                Name = "<no channel>",
                Frequency = -1,
                ProgramMapPID = -1
            };
            var lastChannel = new DVBTChannel()
            {
                Name = "<last channel>",
                Frequency = 0,
                ProgramMapPID = 0
            };

            var channels = await _channelService.LoadChannels();
            if (channels.Count == 0)
            {
                SelectedChannel = noChannel;
                return;
            }

            AutoPlayChannels.Add(noChannel);
            AutoPlayChannels.Add(lastChannel);

            var anythingSelected = false;

            foreach (var ch in channels)
            {
                AutoPlayChannels.Add(ch);

                if (ch.FrequencyAndMapPID == Config.ChannelAutoPlayedAfterStart)
                {
                    anythingSelected = true;
                    SelectedChannel = ch;
                }
            }

            if (!anythingSelected && (!string.IsNullOrEmpty(Config.ChannelAutoPlayedAfterStart)))
            {
                if (Config.ChannelAutoPlayedAfterStart == noChannel.FrequencyAndMapPID)
                {
                    SelectedChannel = noChannel;
                }
                else
                {
                    SelectedChannel = lastChannel;
                }
            } else
            {
                SelectedChannel = noChannel;
            }

            OnPropertyChanged(nameof(AutoPlayChannels));
        }

        public DVBTChannel SelectedChannel
        {
            get
            {
                return _selectedChannel;
            }
            set
            {
                _selectedChannel = value;

                if (value != null)
                    Config.ChannelAutoPlayedAfterStart = value.FrequencyAndMapPID;

                OnPropertyChanged(nameof(SelectedChannel));
            }
        }

        public string AndroidChannelsListPath
        {
            get
            {
                return Path.Combine(BaseViewModel.AndroidAppDirectory, "DVBTTelevizor.channels.json");
            }
        }

        private async Task ShareLog()
        {
            try
            {
                _loggingService.Info("Sharing channels list");

                var chs = await _channelService.LoadChannels();
                if (chs.Count == 0)
                {
                    await _dialogService.Information("Channel list is empty");
                    return;
                }

                var shareDir = Path.Combine(BaseViewModel.AndroidAppDirectory, "shared");
                if (!Directory.Exists(shareDir))
                {
                    Directory.CreateDirectory(shareDir);
                }

                var listPath = Path.Combine(shareDir, "DVBTTelevizor.channels.json");

                if (File.Exists(listPath))
                {
                    File.Delete(listPath);
                }

                File.WriteAllText(listPath, JsonConvert.SerializeObject(chs));

                MessagingCenter.Send(listPath, BaseViewModel.MSG_ShareFile);
            } catch (Exception ex)
            {
                _loggingService.Error(ex);

                MessagingCenter.Send($"File sharing failed", BaseViewModel.MSG_ToastMessage);
            }
        }

        public bool IsFullScreen
        {
            get
            {
                return Config.Fullscreen;
            }
            set
            {
                Config.Fullscreen = value;
                if (value)
                {
                    MessagingCenter.Send(String.Empty, BaseViewModel.MSG_EnableFullScreen);
                }
                else
                {
                    MessagingCenter.Send(String.Empty, BaseViewModel.MSG_DisableFullScreen);
                }

                OnPropertyChanged(nameof(IsFullScreen));
            }
        }

        public int AppFontSizeIndex
        {
            get
            {
                return (int)_config.AppFontSize;
            }
            set
            {
                _config.AppFontSize = (AppFontSizeEnum)value;

                OnPropertyChanged(nameof(AppFontSizeIndex));
                NotifyFontSizeChange();
            }
        }

        public bool EnableLogging
        {
            get
            {
                return _config.EnableLogging;
            }
            set
            {
                if (!value)
                {
                    _config.EnableLogging = false;
                } else
                {
                    ActivateLogging();
                }

                OnPropertyChanged(nameof(EnableLogging));
            }
        }

        private void ActivateLogging()
        {
            if (!_config.EnableLogging)
            {
                _config.EnableLogging = true;
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await _dialogService.Information("Logging will be enabled after application restart");
                });
            }
        }

        private async Task Export()
        {
            try
            {
                _loggingService.Info($"Exporting channels");

                var chs = await _channelService.LoadChannels();
                if (chs.Count == 0)
                {
                    await _dialogService.Information("Channel list is empty");
                    return;
                }

                if (File.Exists(AndroidChannelsListPath))
                {
                    if (!await _dialogService.Confirm($"File {AndroidChannelsListPath} exists. Overwite?"))
                    {
                        return;
                    }

                    File.Delete(AndroidChannelsListPath);
                }

                File.WriteAllText(AndroidChannelsListPath, JsonConvert.SerializeObject(chs));

                MessagingCenter.Send($"File exported.", BaseViewModel.MSG_ToastMessage);

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Export failed");
                await _dialogService.Error($"Export failed");
            }
        }

        private async Task Import()
        {
            try
            {
                _loggingService.Info($"Importing channels from file");

                var chs = await _channelService.LoadChannels();

                if (!File.Exists(AndroidChannelsListPath))
                {
                    await _dialogService.Error($"File {AndroidChannelsListPath} not found");
                    return;
                }

                var jsonFromFile = File.ReadAllText(AndroidChannelsListPath);

                var importedChannels = JsonConvert.DeserializeObject<ObservableCollection<DVBTChannel>>(jsonFromFile);

                var count = 0;
                foreach (var ch in importedChannels)
                {
                    if (!ConfigViewModel.ChannelExists(chs, ch.FrequencyAndMapPID))
                    {
                        count++;
                        ch.Number = ConfigViewModel.GetNextChannelNumber(chs).ToString();
                        chs.Add(ch);
                    }
                }

                await _channelService.SaveChannels(chs);

                MessagingCenter.Send($"Imported channels count: {count}", BaseViewModel.MSG_ToastMessage);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Import failed");
                await _dialogService.Error($"Import failed");
            }
        }

        private async Task ClearChannels()
        {
            _loggingService.Info($"Clearing channels");

            var chs = await _channelService.LoadChannels();
            if (chs.Count == 0)
            {
                await _dialogService.Information("Channel list is empty");
                return;
            }

            if (await _dialogService.Confirm($"Are you sure to delete all channels ({chs.Count})?"))
            {
                await _channelService.SaveChannels(new System.Collections.ObjectModel.ObservableCollection<DVBTChannel>());
                _config.ChannelAutoPlayedAfterStart = null;
                _config.SelectedChannelFrequencyAndMapPID = null;

                MessagingCenter.Send(String.Empty, BaseViewModel.MSG_ClearCache);

                MessagingCenter.Send("Channels deleted", BaseViewModel.MSG_ToastMessage);
            }
        }

        private async Task ClearEPG()
        {
            _loggingService.Info($"Clearing cahce");

            if (await _dialogService.Confirm($"Are you sure to clear all EPG and PID cache?"))
            {
                MessagingCenter.Send(String.Empty, BaseViewModel.MSG_ClearCache);

                MessagingCenter.Send("Cache cleared", BaseViewModel.MSG_ToastMessage);
            }
        }

        protected async Task AcknowledgePurchase(string token)
        {
            _loggingService.Info($"Acknowledge purchase token: {token}");

            try
            {
                var acknowledged = await CrossInAppBilling.Current.FinalizePurchaseAsync(token);

                foreach (var purchase in acknowledged)
                {
                    if (purchase.Success)
                    {
                        HideDonateButton(purchase.Id);
                        _loggingService.Info($"Successfully acknowledged");
                    }
                    else
                    {
                        _loggingService.Info($"Acknowledge failed");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Acknowledge error");
            }
        }

        public async Task AcknowledgePurchases()
        {
            _loggingService.Info($"AcknowledgePurchases");

            try
            {
                // contacting service

                var connected = await CrossInAppBilling.Current.ConnectAsync();

                if (!connected)
                {
                    _loggingService.Info($"Connection to AppBilling service failed");
                    return;
                }

                var purchases = await CrossInAppBilling.Current.GetPurchasesAsync(ItemType.InAppPurchase);
                foreach (var purchase in purchases)
                {
                    if (purchase.IsAcknowledged.HasValue && !purchase.IsAcknowledged.Value)
                    {
                        await AcknowledgePurchase(purchase.PurchaseToken);

                        _loggingService.Info($"Purchase AutoRenewing: {purchase.AutoRenewing}");
                        _loggingService.Info($"Purchase Payload: {purchase.Payload}");
                        _loggingService.Info($"Purchase PurchaseToken: {purchase.PurchaseToken}");
                        _loggingService.Info($"Purchase State: {purchase.State}");
                        _loggingService.Info($"Purchase TransactionDateUtc: {purchase.TransactionDateUtc}");
                        _loggingService.Info($"Purchase ConsumptionState: {purchase.ConsumptionState}");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while acknowledge purchases");
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }
        }

        protected async Task Donate(string productId)
        {
           try
           {
                _loggingService.Debug($"Paying product id: {productId}");

                var connected = await CrossInAppBilling.Current.ConnectAsync();

                if (!connected)
                {
                    _loggingService.Info($"Connection to AppBilling service failed");
                    await _dialogService.Information("Connection to billing service failed");
                    return;
                }

                var purchase = await CrossInAppBilling.Current.PurchaseAsync(productId, ItemType.InAppPurchase);
                if (purchase == null)
                {
                    _loggingService.Info($"Not purchased");
                }
                else
                {
                    _loggingService.Info($"Purchase OK");

                    _loggingService.Info($"Purchase Id: {purchase.Id}");
                    _loggingService.Info($"Purchase Token: {purchase.PurchaseToken}");
                    _loggingService.Info($"Purchase State: {purchase.State.ToString()}");
                    _loggingService.Info($"Purchase Date: {purchase.TransactionDateUtc.ToString()}");
                    _loggingService.Info($"Purchase Payload: {purchase.Payload}");
                    _loggingService.Info($"Purchase ConsumptionState: {purchase.ConsumptionState.ToString()}");
                    _loggingService.Info($"Purchase AutoRenewing: {purchase.AutoRenewing}");

                    if (purchase.State == PurchaseState.Purchased)
                    {
                        await AcknowledgePurchase(purchase.PurchaseToken);
                    }
                    else if (purchase.State == PurchaseState.PaymentPending)
                    {
                        await _dialogService.Information($"Payment is pending.");
                    }
                    else
                    {
                        await _dialogService.Error($"Payment failed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Payment failed");

                //await _dialogService.Information($"Payment failed");
            }
            finally
            {
                await CrossInAppBilling.Current.DisconnectAsync();
            }

            await CheckPurchases();
        }

        public bool AllowRemoteAccessService
        {
            get
            {
                return Config.AllowRemoteAccessService;
            }
            set
            {
                Config.AllowRemoteAccessService = value;

                OnPropertyChanged(nameof(Config));
                MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_SettingsPageForceLayout);
            }
        }
    }
}
