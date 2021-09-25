using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Xamarin.Forms;
using Android.Content;
using System.Threading.Tasks;
using Android.Net.Wifi;

using VideoView = LibVLCSharp.Platforms.Android.VideoView;
using LibVLCSharp.Shared;
using Plugin.Permissions;
using LoggerService;
using Plugin.Permissions.Abstractions;
using Plugin.CurrentActivity;
using Plugin.Toast;
using System.IO;

namespace DVBTTelevizor.Droid
{
    [Activity(Label = "DVBTTelevizor", Icon = "@drawable/Icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private const int StartRequestCode = 1000;
        bool _waitingForInit = false;
        ILoggingService _loggingService;
        DVBTTelevizorConfiguration _config;
        App _app;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            CrossCurrentActivity.Current.Activity = this;

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);

            // workaround for not using FileProvider:
            // https://stackoverflow.com/questions/38200282/android-os-fileuriexposedexception-file-storage-emulated-0-test-txt-exposed
            StrictMode.VmPolicy.Builder builder = new StrictMode.VmPolicy.Builder();
            StrictMode.SetVmPolicy(builder.Build());

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            _config = new DVBTTelevizorConfiguration();

            InitLogging().Wait();

#if DEBUG
            _config.ShowServiceMenu = true;
            if (_loggingService is BasicLoggingService)
                (_loggingService as BasicLoggingService).MinLevel = LoggingLevelEnum.Debug;
#endif

            // prevent sleep:
            Window window = (Forms.Context as Activity).Window; 
            window.AddFlags(WindowManagerFlags.KeepScreenOn);

            _app = new App(_loggingService, _config);
            LoadApplication(_app);

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_Init, (message) =>
            {
                try
                {

                    _loggingService.Info("Initializing DVBT driver");

                    var req = new Intent(Intent.ActionView);
                    req.SetData(new Android.Net.Uri.Builder().Scheme("dtvdriver").Build());
                    req.PutExtra(Intent.ExtraReturnResult, true);

                    _waitingForInit = true;

                    Task.Run( () =>
                    {
                        System.Threading.Thread.Sleep(5000); // wait 5 secs;

                        if (_waitingForInit)
                        {
                            _waitingForInit = false;
                            _loggingService.Error("Driver response timeout");
                            MessagingCenter.Send("Driver response timeout", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                        }

                    });

                    StartActivityForResult(req, StartRequestCode);

                }
                catch (ActivityNotFoundException ex)
                {
                    _waitingForInit = false;
                    _loggingService.Error(ex, "Driver initializing failed");

                    MessagingCenter.Send("DVB-T driver not installed", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                }
                catch (Exception ex)
                {
                    _waitingForInit = false;
                    _loggingService.Error(ex,"Driver initializing failed");

                    MessagingCenter.Send("Driver initializing failed", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                }
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ToastMessage, (message) =>
            {
                CrossToastPopUp.Current.ShowCustomToast(message, "#0000FF", "#FFFFFF");
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ShareFile, (fileName) =>
            {
                ShareFile(fileName);
            });
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == StartRequestCode && resultCode == Result.Ok)
            {
                _waitingForInit = false;

                if (data != null)
                {
                    var cfg = new DVBTDriverConfiguration();

                    if (data.HasExtra("ControlPort"))
                        cfg.ControlPort = data.GetIntExtra("ControlPort", 0);

                    if (data.HasExtra("TransferPort"))
                        cfg.TransferPort = data.GetIntExtra("TransferPort", 0);

                    if (data.HasExtra("DeviceName"))
                        cfg.DeviceName = data.GetStringExtra("DeviceName");

                    if (data.HasExtra("ProductIds"))
                        cfg.ProductIds = data.GetIntArrayExtra("ProductIds");

                    if (data.HasExtra("VendorIds"))
                        cfg.VendorIds = data.GetIntArrayExtra("VendorIds");

                    _loggingService.Info($"Received DVBT driver configuration: {cfg}");

                    MessagingCenter.Send(cfg.ToString(), BaseViewModel.MSG_DVBTDriverConfiguration);
                } else
                {
                    MessagingCenter.Send("No response from driver", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                }
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async Task InitLogging()
        {
            var permitted = await CrossPermissions.Current.CheckPermissionStatusAsync<StoragePermission>();

            if (permitted == PermissionStatus.Granted && _config.EnableLogging)
            {
                var logPath = Path.Combine(BaseViewModel.ExternalStorageDirectory, "DVBTTelevizor.log.txt");

                _loggingService = new FileLoggingService()
                {
                    LogFilename = logPath
                };

                _loggingService.Debug("File logger initialized");
            } else
            {
                _loggingService = new BasicLoggingService();
            }
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            MessagingCenter.Send(keyCode.ToString(), BaseViewModel.MSG_KeyDown);

            return base.OnKeyDown(keyCode, e);
        }

        private async Task ShareFile(string fileName)
        {
            try
            {
                var intent = new Intent(Intent.ActionSend);
                var file = new Java.IO.File(fileName);
                var uri = Android.Net.Uri.FromFile(file);

                intent.PutExtra(Intent.ExtraStream, uri);
                intent.SetDataAndType(uri, "text/plain");
                intent.SetFlags(ActivityFlags.GrantReadUriPermission);
                intent.SetFlags(ActivityFlags.NewTask);

                Android.App.Application.Context.StartActivity(intent);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        protected override void OnDestroy()
        {
            _app.Done();

            base.OnDestroy();
        }
    }
}