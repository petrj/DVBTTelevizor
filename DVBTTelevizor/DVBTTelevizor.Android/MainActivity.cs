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
using LoggerService;
using System.IO;
using Android.Support.Design.Widget;
using Xamarin.Essentials;
using Android.Hardware.Usb;
using Android.Support.V4.Content;
using Android;
using Android.Support.V4.App;
using System.Text;
using Plugin.InAppBilling;

namespace DVBTTelevizor.Droid
{
    [Activity(Label = "DVBT Televizor", Name= "net.petrjanousek.DVBTTelevizor.MainActivity", Icon = "@drawable/Icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] { Intent.ActionView, Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeType = "*/*", DataSchemes = new[] { "file", "content" }, DataPathPattern = ".*\\.json")]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        public static bool InstanceAlreadyStarted = false;

        private const int StartRequestCode = 1000;
        private bool _waitingForInit = false;
        private ILoggingService _loggingService;
        private DVBTTelevizorConfiguration _config;
        private int _fullscreenUiOptions;
        private int _defaultUiOptions;
        private App _app;
        private IDVBTDriverManager _driverManager;
        NotificationHelper _notificationHelper;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            if (Intent != null &&
                InstanceAlreadyStarted &&
                (Intent.Action == Intent.ActionView ||
                Intent.Action == Intent.ActionSend))
            {
                HandleImportFile(Intent);
                Finish();
                return;
            }

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this, savedInstanceState);
            Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity = this;

            _config = new DVBTTelevizorConfiguration();

#if DEBUG
            _config.ShowServiceMenu = true;
            _config.ScanEPG = true;
            _config.EnableLogging = true;
            //_config.AutoInitAfterStart = false;
#endif

            InitLogging();

            _loggingService.Info("DVBTTelevizor starting");

            // workaround for not using FileProvider (necessary for file sharing):
            // https://stackoverflow.com/questions/38200282/android-os-fileuriexposedexception-file-storage-emulated-0-test-txt-exposed
            StrictMode.VmPolicy.Builder builder = new StrictMode.VmPolicy.Builder();
            StrictMode.SetVmPolicy(builder.Build());

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            // prevent sleep:
            Window window = (Forms.Context as Activity).Window;
            window.AddFlags(WindowManagerFlags.KeepScreenOn);

            // https://stackoverflow.com/questions/39248138/how-to-hide-bottom-bar-of-android-back-home-in-xamarin-forms
            _defaultUiOptions = (int)Window.DecorView.SystemUiVisibility;

            _fullscreenUiOptions = _defaultUiOptions;
            _fullscreenUiOptions |= (int)SystemUiFlags.LowProfile;
            _fullscreenUiOptions |= (int)SystemUiFlags.Fullscreen;
            _fullscreenUiOptions |= (int)SystemUiFlags.HideNavigation;
            _fullscreenUiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            if (_config.Fullscreen)
            {
                SetFullScreen(true);
            }

            try
            {
                UsbManager manager = (UsbManager)GetSystemService(Context.UsbService);

                var usbReciever = new USBBroadcastReceiverSystem();
                var intentFilter = new IntentFilter(UsbManager.ActionUsbDeviceAttached);
                var intentFilter2 = new IntentFilter(UsbManager.ActionUsbDeviceDetached);
                RegisterReceiver(usbReciever, intentFilter);
                RegisterReceiver(usbReciever, intentFilter2);
                usbReciever.UsbAttachedOrDetached += CheckIfUsbAttachedOrDetached;

            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while initializing UsbManager");
            }

#if TestingDVBTDriverManager

            _driverManager = new TestingDVBTDriverManager()
            {
                Configuration = new DVBTDriverConfiguration()
                {
                    DeviceName = "Test device"
                }
            };
#else
            _driverManager = new DVBTDriverManager(_loggingService, _config);
#endif

            _notificationHelper = new NotificationHelper(this);

            _app = new App(_loggingService, _config, _driverManager);
            LoadApplication(_app);

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_Init, (message) =>
            {
                InitDriver();
            });

            MessagingCenter.Subscribe<SettingsPage>(this, BaseViewModel.MSG_CheckBatterySettings, (sender) =>
            {
                try
                {
                    var pm = (PowerManager)Android.App.Application.Context.GetSystemService(Context.PowerService);
                    bool ignoring = pm.IsIgnoringBatteryOptimizations(AppInfo.PackageName);

                    if (!ignoring)
                    {
                        MessagingCenter.Send<string>(string.Empty, BaseViewModel.MSG_RequestBatterySettings);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
            });

            MessagingCenter.Subscribe<SettingsPage>(this, BaseViewModel.MSG_SetBatterySettings, (sender) =>
            {
                try
                {
                    var intent = new Intent();
                    intent.SetAction(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
                    intent.SetFlags(ActivityFlags.NewTask);
                    Android.App.Application.Context.StartActivity(intent);
                }
                catch (Exception ex)
                {
                    _loggingService.Error(ex);
                }
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ToastMessage, (message) =>
            {
                ShowToastMessage(message);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_LongToastMessage, (message) =>
            {
                ShowToastMessage(message, 8000);
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_EnableFullScreen, (msg) =>
            {
                SetFullScreen(true);
            });
            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DisableFullScreen, (msg) =>
            {
                SetFullScreen(false);
            });

            MessagingCenter.Subscribe<MainPage, PlayStreamInfo>(this, BaseViewModel.MSG_PlayInBackgroundNotification, (sender, playStreamInfo) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShowPlayingNotification(playStreamInfo));
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_StopPlayInBackgroundNotification, (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    StopPlayingNotification();
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ShareFile, (fileName) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShareFile(fileName));
                });
            });

            InstanceAlreadyStarted = true;

            if (Intent != null &&
                InstanceAlreadyStarted &&
                (Intent.Action == Intent.ActionView ||
                Intent.Action == Intent.ActionSend))
            {
                HandleImportFile(Intent);
            }
        }

        private void HandleImportFile(Intent intent)
        {
            try
            {
                using (var inputStream = ContentResolver.OpenInputStream(intent.Data))
                {
                    using (var memStream = new System.IO.MemoryStream())
                    {
                        inputStream.CopyTo(memStream);

                        memStream.Seek(0, SeekOrigin.Begin);

                        using (var streamReader = new StreamReader(memStream))
                        {
                            string json = streamReader.ReadToEnd();
                            MessagingCenter.Send(json, BaseViewModel.MSG_ImportChannelsList);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
                ShowToastMessage("json import error");
            }
        }

        private void InitDriver()
        {
            try
            {
                if (_driverManager.Started)
                {
                    return;
                }

                _loggingService.Info("Initializing device");

                var req = new Intent(Intent.ActionView);
                req.SetData(new Android.Net.Uri.Builder().Scheme("dtvdriver").Build());
                req.PutExtra(Intent.ExtraReturnResult, true);

                _waitingForInit = true;

                Task.Run(async () =>
                {
                    await Task.Delay(5000); // wait 5 secs;

                    if (_waitingForInit)
                    {
                        _waitingForInit = false;

                        _loggingService.Error("Device response timeout");

                        MessagingCenter.Send("response timeout", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                    }

                });

                StartActivityForResult(req, StartRequestCode);

#if TestingDVBTDriverManager

                var cfg = new DVBTDriverConfiguration()
                {
                    DeviceName = "Testing device"
                };

                MessagingCenter.Send(cfg.ToString(), BaseViewModel.MSG_DVBTDriverConfiguration);
#endif

            }
            catch (ActivityNotFoundException ex)
            {
                _waitingForInit = false;
                _loggingService.Error(ex, "Device initializing failed");

                MessagingCenter.Send("DVBT driver not installed", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
            }
            catch (Exception ex)
            {
                _waitingForInit = false;
                _loggingService.Error(ex, "Driver initializing failed");

                MessagingCenter.Send("connection failed", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
            }
        }

        private async void CheckIfUsbAttachedOrDetached(object sender, EventArgs e)
        {
            // TODO: detect device that has been attached

            if (!_driverManager.Started)
            {
                InitDriver();
            } else
            {
                var status = await _driverManager.CheckStatus();

                if (!status)
                {
                    await _driverManager.Disconnect();

                    MessagingCenter.Send("connection failed", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                }
            }
        }


        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            InAppBillingImplementation.HandleActivityResult(requestCode, resultCode, data);

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

                    _loggingService.Info($"Received device configuration: {cfg}");

                    MessagingCenter.Send(cfg.ToString(), BaseViewModel.MSG_DVBTDriverConfiguration);
                } else
                {
                    MessagingCenter.Send("no response", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                }
            }
        }

        private void InitLogging()
        {
            if (_config.EnableLogging)
            {
                var logPath = Path.Combine(BaseViewModel.AndroidAppDirectory, "DVBTTelevizor.log.txt");

                _loggingService = new FileLoggingService()
                {
                    LogFilename = logPath
                };

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

        private void SetFullScreen(bool on)
        {
            try
            {
                if (on)
                {
                    Window.DecorView.SystemUiVisibility = (StatusBarVisibility)_fullscreenUiOptions;
                }
                else
                {
                    Window.DecorView.SystemUiVisibility = (StatusBarVisibility)_defaultUiOptions;
                };
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void ShowToastMessage(string message, int duration = 0)
        {
            try
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    var view = FindViewById(Android.Resource.Id.Content);

                    var snackBar = Snackbar.Make(view, message, Snackbar.LengthLong);

                    var textView = snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text);

                    var minTextSize = textView.TextSize; // 16

                    textView.SetTextColor(Android.Graphics.Color.White);

                    var screenHeightRate = 0;

                    /*
                            appFontSize:

                            Normal = 0,
                            AboveNormal = 1,
                            Big = 2,
                            Biger = 3,
                            VeryBig = 4,
                            Huge = 5

                          */


                    if (DeviceDisplay.MainDisplayInfo.Height < DeviceDisplay.MainDisplayInfo.Width)
                    {
                        // Landscape

                        screenHeightRate = Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height / 16.0);
                        textView.SetMaxLines(2);
                    }
                    else
                    {
                        // Portrait

                        screenHeightRate = Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height / 32.0);
                        textView.SetMaxLines(4);
                    }

                    var fontSizeRange = screenHeightRate - minTextSize;
                    var fontSizePerValue = fontSizeRange / 5;

                    var fontSize = minTextSize + (int)_config.AppFontSize * fontSizePerValue;

                    textView.SetTextSize(Android.Util.ComplexUnitType.Px, Convert.ToSingle(fontSize));

                    if (duration != 0)
                    {
                        snackBar.SetDuration(duration);
                    }

                    snackBar.Show();
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
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

        private async Task ShowPlayingNotification(PlayStreamInfo playStreamInfo)
        {
            try
            {
                var msg = playStreamInfo == null || playStreamInfo.Channel == null ? "" : $"Playing {playStreamInfo.Channel.Name}";
                _notificationHelper.ShowNotification(String.Empty, msg , String.Empty);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void StopPlayingNotification()
        {
            try
            {
                _notificationHelper.CloseNotification();
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        protected override void OnDestroy()
        {
            if (_app != null)
                _app.Done();

            base.OnDestroy();
        }
    }
}