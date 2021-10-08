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
using System.IO;

namespace DVBTTelevizor
{
    public class BaseViewModel : ConfigViewModel
    {
        protected ILoggingService _loggingService;
        protected IDialogService _dialogService;
        protected DVBTDriverManager _driver;

        public const string MSG_DVBTDriverConfiguration = "DVBTDriverConfiguration";
        public const string MSG_DVBTDriverConfigurationFailed = "DVBTDriverConfigurationFailed";
        public const string MSG_EnableFullScreen = "EnableFullScreen";
        public const string MSG_DisableFullScreen = "DisableFullScreen";
        public const string MSG_PlayStream = "PlayStream";
        public const string MSG_UpdateDriverState = "UpdateDriverState";
        public const string MSG_Init = "Init";
        public const string MSG_KeyDown = "KeyDown";
        public const string MSG_ToastMessage = "ShowToastMessage";
        public const string MSG_EditChannel = "EditChannel";
        public const string MSG_ShareFile = "ShareFile";
        public const string MSG_CheckBatterySettings = "CheckBatterySettings";
        public const string MSG_RequestBatterySettings = "RequestBatterySettings";
        public const string MSG_SetBatterySettings = "SetBatterySettings ";

        private string _status;

        bool isBusy = false;

        public BaseViewModel(ILoggingService loggingService, IDialogService dialogService, DVBTDriverManager driver, DVBTTelevizorConfiguration config)
              : base(config)
        {
            _loggingService = loggingService;
            _dialogService = dialogService;
            _driver = driver;

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                do
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(
                        new Action(
                            delegate
                            {
                                OnPropertyChanged(nameof(DataStreamInfo));
                            }));

                    // 2 secs delay
                    Thread.Sleep(2 * 1000);

                } while (true);
            }).Start();
        }

        // cannot run async!
        public void ConnectDriver(string message)
        {
            _driver.Configuration = JsonConvert.DeserializeObject<DVBTDriverConfiguration>(message);
            _driver.Start();
            Status = $"Initialized ({_driver.Configuration.DeviceName})";

            MessagingCenter.Send("", BaseViewModel.MSG_UpdateDriverState);
        }

        public async Task DisconnectDriver()
        {
            await _driver.Disconnect();
            Status = $"Not initialized";

            UpdateDriverState();
        }

        public void UpdateDriverState()
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(DriverConnected));
            OnPropertyChanged(nameof(DriverDisConnected));
            OnPropertyChanged(nameof(DriverConnectedIcon));
        }

        public string DataStreamInfo
        {
            get
            {
                return _driver.DataStreamInfo;
            }
        }

        public bool DriverConnected
        {
            get
            {
                return _driver.Started;
            }
        }

        public string DriverConnectedIcon
        {
            get
            {
                if (DriverConnected)
                    return "Connected.png";

                return "Disconnected.png";
            }
        }

        public bool DriverDisConnected
        {
            get
            {
                return !DriverConnected;
            }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set
            {
                //SetProperty(ref isBusy, value);
                //isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public string Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public static bool ChannelExists(ObservableCollection<DVBTChannel> channels, long frequency, string name, long ProgramMapPID)
        {
            foreach (var ch in channels)
            {
                if (ch.Frequency == frequency &&
                    ch.Name == name &&
                    ch.ProgramMapPID == ProgramMapPID)
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> RunWithStoragePermission(Func<Task> action, IDialogService dialogService)
        {
            try
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();

                if (status != PermissionStatus.Granted)
                {
                    if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Storage))
                    {
                        await dialogService.Information("Application requires storage permission", "Information");
                    }

                    status = await CrossPermissions.Current.RequestPermissionAsync<StoragePermission>();
                }

                if (status == PermissionStatus.Granted)
                {
                    await action();
                    return true;
                }
                else
                {
                    await dialogService.Error("Storage permission not granted", "Error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static string DownloadDirectory
        {
            get
            {
                var downloadFolderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
                if (!Directory.Exists(downloadFolderPath.AbsolutePath))
                {
                    Directory.CreateDirectory(downloadFolderPath.AbsolutePath);
                }
                return downloadFolderPath.AbsolutePath;
            }
        }

        public static string MovieDirectory
        {
            get
            {
                var folderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies);
                if (!Directory.Exists(folderPath.AbsolutePath))
                {
                    Directory.CreateDirectory(folderPath.AbsolutePath);
                }
                return folderPath.AbsolutePath;
            }
        }

        public static string ExternalStorageDirectory
        {
            get
            {
                return Android.OS.Environment.ExternalStorageDirectory.Path;
            }
        }
    }
}
