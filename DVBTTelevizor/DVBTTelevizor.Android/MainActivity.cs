using System;
using Android.App;
using Android.Content.PM;
using Android.Views;
using Android.Widget;
using Android.OS;
using Xamarin.Forms;
using Android.Content;
using System.Threading.Tasks;
using LoggerService;
using System.IO;
using Xamarin.Essentials;
using Android.Hardware.Usb;
using Google.Android.Material.Snackbar;
using Plugin.CurrentActivity;
using DVBTTelevizor.Services;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media.TV;
using static Google.Android.Material.Snackbar.Snackbar;

namespace DVBTTelevizor.Droid
{
    [Activity(Label = "DVBT Televizor", Name= "net.petrjanousek.DVBTTelevizor.MainActivity", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = false, Exported = false, Banner = "@drawable/banner", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    [IntentFilter(new[] { Intent.ActionView, Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryLeanbackLauncher }, DataMimeType = "*/*", DataSchemes = new[] { "file", "content" }, DataPathPattern = ".*\\.json")]
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
        private NotificationHelper _notificationHelper;
        private static Android.Widget.Toast _instance;

        private bool _dispatchKeyEventEnabled = false;
        private DateTime _dispatchKeyEventEnabledAt = DateTime.MaxValue;
        //private bool _driverInitializing = false;

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

            CrossCurrentActivity.Current.Init(this, savedInstanceState);

            _config = new DVBTTelevizorConfiguration();

#if DEBUG
            _config.EnableLogging = true;
#endif

            InitLogging();

            _loggingService.Info("DVBTTelevizor starting");

            // workaround for not using FileProvider (necessary for file sharing):
            // https://stackoverflow.com/questions/38200282/android-os-fileuriexposedexception-file-storage-emulated-0-test-txt-exposed
            StrictMode.VmPolicy.Builder builder = new StrictMode.VmPolicy.Builder();
            StrictMode.SetVmPolicy(builder.Build());

            // https://github.com/xamarin/Xamarin.Forms/issues/15582
            Xamarin.Forms.Forms.SetFlags("Disable_Accessibility_Experimental");

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            var context = Platform.AppContext;
            var activity = Platform.CurrentActivity;

            // prevent sleep:
            Window window = (Forms.Context as Activity).Window;
            window.AddFlags(WindowManagerFlags.KeepScreenOn);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

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

            _driverManager = new TestingDVBTDriverManager();
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

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_DisableDispatchKeyEvent, (message) =>
            {
                _dispatchKeyEventEnabled = false;
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_EnableDispatchKeyEvent, (message) =>
            {
                _dispatchKeyEventEnabledAt = DateTime.Now;
                _dispatchKeyEventEnabled = true;
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

            MessagingCenter.Subscribe<PlayStreamInfo>(this, BaseViewModel.MSG_ShowRecordNotification, (playStreamInfo) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShowRecordNotification(playStreamInfo));
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_CloseRecordNotification, (msg) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    StopNotification(2);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_StopPlayInBackgroundNotification, (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    StopNotification(1);
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_ShareFile, (fileName) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShareFile(fileName));
                });
            });

            MessagingCenter.Subscribe<string>(string.Empty, BaseViewModel.MSG_QuitApp, (sender) =>
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    StopNotification(1);
                    StopNotification(2);
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                });
            });

            MessagingCenter.Subscribe<string>(this, BaseViewModel.MSG_RemoteKeyAction, (code) =>
            {
                SendRemoteKey(code);
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

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _loggingService.Error(e.ExceptionObject as Exception, "CurrentDomain_UnhandledException");
        }

        private void SendRemoteKey(string code)
        {
            _loggingService.Debug($"SendRemoteKey: {code}");

            Android.Views.Keycode keyCode;
            if (Enum.TryParse<Android.Views.Keycode>(code, out keyCode))
            {
                new Instrumentation().SendKeyDownUpSync(keyCode);
            }
            else
            {
                _loggingService.Info("SendRemoteKey: invalid key code");
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
            //if (_driverInitializing)
            //{
            //    return;
            //}

            try
            {
                if (_driverManager.Connected)
                {
                    return;
                }

                //_driverInitializing = true;

                _loggingService.Info("Initializing device");


#if TestingDVBTDriver

                _driverManager.Installed = true;
                var testingDVBTDriver = new TestingDVBTDriver(_loggingService);
                testingDVBTDriver.Connect();

                var cfg = new DVBTDriverConfiguration()
                {
                    DeviceName = "Testing DVBT driver",
                    ControlPort = testingDVBTDriver.ControlIPEndPoint.Port,
                    TransferPort = testingDVBTDriver.TransferIPEndPoint.Port
                };

                MessagingCenter.Send(cfg.ToString(), BaseViewModel.MSG_DVBTDriverConfiguration);

                return;
#endif

                var req = new Intent(Intent.ActionView);
                req.SetData(new Android.Net.Uri.Builder().Scheme("dtvdriver").Build());
                req.PutExtra(Intent.ExtraReturnResult, true);

                _waitingForInit = true;

                Task.Run(async () =>
                {
                    await Task.Delay(10000); // wait 10 secs;

                    if (_waitingForInit)
                    {
                        _waitingForInit = false;

                        _loggingService.Info("Device response timeout");

                        MessagingCenter.Send("response timeout", BaseViewModel.MSG_DVBTDriverConfigurationFailed);
                    }

                });

                StartActivityForResult(req, StartRequestCode);

                // updating driver installed status
                MessagingCenter.Send("", BaseViewModel.MSG_DVBTDriverConfigurationChanged);

#if TestingDVBTDriverManager

                _driverManager.Installed = true;
                _waitingForInit = false;

                var cfg = new DVBTDriverConfiguration()
                {
                    DeviceName = "Testing device"
                };

                MessagingCenter.Send(cfg.ToString(), BaseViewModel.MSG_DVBTDriverConfiguration);
#endif
                _driverManager.Installed = true;
            }
            catch (ActivityNotFoundException ex)
            {
                _driverManager.Installed = false;
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
            //} finally
            //{
            //    _driverInitializing = false;
            //}

        }

        private async void CheckIfUsbAttachedOrDetached(object sender, EventArgs e)
        {
            // TODO: detect device that has been attached

            if (!_driverManager.Connected)
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
                _loggingService = new NLogLoggingService(GetType().Assembly, "DVBTTelevizor.Droid");
            } else
            {
                _loggingService = new DummyLoggingService();
            }
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return base.OnKeyDown(keyCode, e);
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            try
            {
                if (e.Action == KeyEventActions.Up)
                {
                    return true;
                }

                var code = e.KeyCode.ToString();
                var keyAction = KeyboardDeterminer.GetKeyAction(code);

                if (e.IsLongPress)
                {
                    code = $"{BaseViewModel.LongPressPrefix}{e.KeyCode.ToString()}";
                }

                if (_dispatchKeyEventEnabled)
                {
                    // ignoring ENTER 1 second after DispatchKeyEvent enabled

                    var ms = (DateTime.Now - _dispatchKeyEventEnabledAt).TotalMilliseconds;

                    if (keyAction == KeyboardNavigationActionEnum.OK && ms < 1000)
                    {
                        _loggingService.Debug($"DispatchKeyEvent: {code} -> ignoring OK action");

                        return true;
                    }
                    else
                    {
                        _loggingService.Debug($"DispatchKeyEvent: {code} -> sending to ancestor");
                        return base.DispatchKeyEvent(e);
                    }
                }
                else
                {
                    if (keyAction != KeyboardNavigationActionEnum.Unknown)
                    {
                        _loggingService.Debug($"DispatchKeyEvent: {code} -> sending to application, time: {e.EventTime - e.DownTime}");

                        MessagingCenter.Send(code, BaseViewModel.MSG_KeyDown);

                        return true;
                    }
                    else
                    {
                        // unknown key

                        _loggingService.Debug($"DispatchKeyEvent: {code} -> unknown key sending to ancestor");
#if DEBUG
                        ShowToastMessage($"<{code}>");
#endif
                        return base.DispatchKeyEvent(e);
                    }
                }
            } catch (Exception ex)
            {
                _loggingService.Error(ex, $"DispatchKeyEvent error:");
                return true;
            }
        }

        private void SetFullScreen(bool on)
        {
            //_loggingService.Info($"SetFullScreen: {on}");

            try
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (on)
                    {
                        Window.DecorView.SystemUiVisibility = (StatusBarVisibility)_fullscreenUiOptions;
                    }
                    else
                    {
                        Window.DecorView.SystemUiVisibility = (StatusBarVisibility)_defaultUiOptions;
                    };
                });
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

                    _instance?.Cancel();
                    _instance = Android.Widget.Toast.MakeText(Android.App.Application.Context, message, ToastLength.Short);

                    TextView textView;
                    Snackbar snackBar = null;

                    var backgroundColor = Android.Graphics.Color.Rgb(38, 38, 38);
                    var textColor = Android.Graphics.Color.Rgb(65, 179, 255);

                    var tView = _instance.View;
                    if (tView == null)
                    {
                        // Since Android 11, custom toast is deprecated - using snackbar instead:

                        var activity = CrossCurrentActivity.Current.Activity;
                        var view = activity.FindViewById(Android.Resource.Id.Content);

                        snackBar = Snackbar.Make(view, message, Snackbar.LengthLong);
                        snackBar.View.Background.SetAlpha(0);

                        textView = snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text);

                        // rounded corners:
                        var pd = new PaintDrawable(backgroundColor);
                        pd.SetCornerRadius(30);
                        pd.SetAlpha(200);
                        textView.Background = pd;

                        // text in center
                        textView.Gravity = GravityFlags.Center;

                        // view in center / autoSize
                        var snackbarLayout = (Snackbar.SnackbarLayout)snackBar.View;
                        var layoutParams = (Snackbar.SnackbarLayout.LayoutParams)snackbarLayout.LayoutParameters;
                        layoutParams.Gravity = GravityFlags.Bottom | GravityFlags.CenterHorizontal;
                        layoutParams.Width = ViewGroup.LayoutParams.WrapContent;
                        snackBar.View.LayoutParameters = layoutParams;
                    }
                    else
                    {
                        // using Toast

                        backgroundColor = Android.Graphics.Color.Rgb(128, 128, 128);

                        tView.Background.SetColorFilter(backgroundColor, PorterDuff.Mode.SrcIn); //Gets the actual oval background of the Toast then sets the color filter
                        textView = (TextView)tView.FindViewById(Android.Resource.Id.Message);
                        textView.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);

                        _instance.SetGravity(GravityFlags.Bottom | GravityFlags.CenterHorizontal, 0, 0);
                    }

                    var minTextSize = textView.TextSize; // 16

                    textView.SetTextColor(textColor);

                    var screenHeightRate = 0;

                    //configuration font size:

                    //Normal = 0,
                    //AboveNormal = 1,
                    //Big = 2,
                    //Biger = 3,
                    //VeryBig = 4,
                    //Huge = 5

                    if (DeviceDisplay.MainDisplayInfo.Height < DeviceDisplay.MainDisplayInfo.Width)
                    {
                        // Landscape

                        screenHeightRate = Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height / 16.0);
                        textView.SetMaxLines(5);
                    }
                    else
                    {
                        // Portrait

                        screenHeightRate = Convert.ToInt32(DeviceDisplay.MainDisplayInfo.Height / 32.0);
                        textView.SetMaxLines(5);
                    }

                    var fontSizeRange = screenHeightRate - minTextSize;
                    var fontSizePerValue = fontSizeRange / 5;

                    var fontSize = minTextSize + (int)_config.AppFontSize * fontSizePerValue;

                    textView.SetTextSize(Android.Util.ComplexUnitType.Px, Convert.ToSingle(fontSize));

                    if (snackBar != null)
                    {
                        snackBar.Show();
                    }
                    else
                    {
                        _instance.Show();
                    }
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
                _notificationHelper.ShowPlayNotification(1, msg, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private async Task ShowRecordNotification(PlayStreamInfo recStreamInfo)
        {
            try
            {
                var msg = recStreamInfo == null || recStreamInfo.Channel == null ? "" : $"Recording {recStreamInfo.Channel.Name}";
                _notificationHelper.ShowRecordNotification(2, msg, string.Empty, string.Empty);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void StopNotification(int notificationId)
        {
            try
            {
                _notificationHelper.CloseNotification(notificationId);
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