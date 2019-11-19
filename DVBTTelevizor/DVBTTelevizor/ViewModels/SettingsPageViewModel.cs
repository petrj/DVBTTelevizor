using LoggerService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class SettingsPageViewModel
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;

        private ChannelService _channelService;

        public Command ClearChannelsCommand { get; set; }
        public Command ExportChannelsCommand { get; set; }
        public Command ImportChannelsCommand { get; set; }

        public SettingsPageViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTTelevizorConfiguration config, ChannelService channelService)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            _channelService = channelService;

            Config = config;

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

        public DVBTTelevizorConfiguration Config { get; set; }

        private async Task Export()
        {
            _loggingService.Info($"Exporting channels");         

            var chs = await _channelService.LoadChannels();
            if (chs.Count == 0)
            {
                await _dialogService.Information("Channel list is empty");
                return;
            }

            var downloadFolderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);            
            var path = Path.Combine(downloadFolderPath.AbsolutePath, "DVBTTelevizor.channels.json");
            if (File.Exists(path))
            {
                if (!await _dialogService.Confirm($"File {path} exists. Overwite?"))
                {
                    return;
                }

                File.Delete(path);
            }
            
            File.WriteAllText(path, JsonConvert.SerializeObject(chs));

            await _dialogService.Information($"File {path} exported.");
        }

        private async Task Import()
        {
            _loggingService.Info($"Importing channels");
 
            var chs = await _channelService.LoadChannels();

            var downloadFolderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
            var path = Path.Combine(downloadFolderPath.AbsolutePath, "DVBTTelevizor.channels.json");
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
                if (!BaseViewModel.ChannelExists(chs, ch.Frequency, ch.Name, ch.ProgramMapPID))
                {
                    count++;
                    chs.Add(ch);
                }
            }
            
            await _channelService.SaveChannels(chs);

            await _dialogService.Information($"Imported channels count: {count}");
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

                await _dialogService.Information($"Channels cleared");
            }
        }
    }
}
