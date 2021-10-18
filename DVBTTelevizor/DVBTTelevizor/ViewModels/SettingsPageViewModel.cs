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

namespace DVBTTelevizor
{
    public class SettingsPageViewModel : ConfigViewModel
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;

        private ChannelService _channelService;

        public Command ClearChannelsCommand { get; set; }
        public Command ExportChannelsCommand { get; set; }
        public Command ImportChannelsCommand { get; set; }

        public SettingsPageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTTelevizorConfiguration config, ChannelService channelService)
            :base(config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            _channelService = channelService;

            _config = config;

            ClearChannelsCommand = new Command(async () => await ClearChannels());

            ExportChannelsCommand = new Command(async () => await BaseViewModel.RunWithStoragePermission(
                async () =>
                {
                    await Export();
                    }, _dialogService));

            ImportChannelsCommand = new Command(async () => await BaseViewModel.RunWithStoragePermission(
                async () =>
                {
                    await Import();
                }, _dialogService));
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
            }
        }

        private void ActivateLogging()
        {
            Task.Run(async ()=>
            {
                return
                    await BaseViewModel.RunWithStoragePermission(
                         async () =>
                         {
                             if (!_config.EnableLogging)
                             {
                                 _config.EnableLogging = true;
                                 await _dialogService.Information("Logging will be enabled after application restart");
                             }

                         }, _dialogService);
            });
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

                var path = Path.Combine(BaseViewModel.GetAndroidMediaDirectory(_config), "DVBTTelevizor.channels.json");
                if (File.Exists(path))
                {
                    if (!await _dialogService.Confirm($"File {path} exists. Overwite?"))
                    {
                        return;
                    }

                    File.Delete(path);
                }

                File.WriteAllText(path, JsonConvert.SerializeObject(chs));

                MessagingCenter.Send($"File {path} exported.", BaseViewModel.MSG_ToastMessage);

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
                _loggingService.Info($"Importing channels");

                var chs = await _channelService.LoadChannels();

                var path = Path.Combine(BaseViewModel.GetAndroidMediaDirectory(_config), "DVBTTelevizor.channels.json");
                if (!File.Exists(path))
                {
                    await _dialogService.Error($"File {path} does not exist.");
                    return;
                }

                var jsonFromFile = File.ReadAllText(path);

                var importedChannels = JsonConvert.DeserializeObject<ObservableCollection<DVBTChannel>>(jsonFromFile);

                var count = 0;
                foreach (var ch in importedChannels)
                {
                    if (!ConfigViewModel.ChannelExists(chs, ch.Frequency, ch.ProgramMapPID))
                    {
                        count++;
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

            if (await _dialogService.Confirm($"Are you sure to clear all channels ({chs.Count})?"))
            {
                await _channelService.SaveChannels(new System.Collections.ObjectModel.ObservableCollection<DVBTChannel>());

                MessagingCenter.Send("Channels cleared", BaseViewModel.MSG_ToastMessage);
            }
        }
    }
}
