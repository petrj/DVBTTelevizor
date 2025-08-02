using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware.Usb;
using Android.Media;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using Google.Android.Material.Snackbar;
using Java.Sql;
using LoggerService;
using Microsoft.Maui.Controls.Compatibility;
using NLog;
using RTLSDR;
using System.ComponentModel;
using System.Net.Sockets;
using System.Net;
using System.Reflection;
using Environment = System.Environment;
using RTLSDR.Common;

namespace DVBTTelevizor.MAUI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int StartRequestCode = 1000;
        private const int StartRequestCodeRTLSDR = 1001;
        private int _audioSampleRate = 96000;
        private int _audioChannels = 1;
        private bool _startAudioReceiverThread = false;
        private int _SDRDriverStreamPort = 0;
        private int _SDRDriverPort = 0;
        private int _audioRecieverPort = 8012;

        private bool _waitingForInit = false;
        private static Android.Widget.Toast _instance;
        private ILoggingService _loggingService = null;

        private bool _dispatchKeyEventEnabled = false;
        private DateTime _dispatchKeyEventEnabledAt = DateTime.MaxValue;
        private NotificationHelper _notificationHelper;

        private BackgroundWorker _audioReceiver;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Assembly assembly = typeof(App).GetTypeInfo().Assembly;
            NLog.Config.ISetupBuilder setupBuilder = NLog.LogManager.Setup();
            NLog.Config.ISetupBuilder configuredSetupBuilder = setupBuilder.LoadConfigurationFromAssemblyResource(assembly);
            _loggingService = new NLogLoggingService(configuredSetupBuilder.GetCurrentClassLogger());

            // prevent sleep:
            var  win = (this as Activity).Window;
            win.AddFlags(WindowManagerFlags.KeepScreenOn);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; ;

            SubscribeMessages();

            var dir = GetAndroidDirectory(null);

            try
            {
                UsbManager manager = (UsbManager)GetSystemService(Context.UsbService);

                var usbReciever = new USBBroadcastReceiverSystem();
                var intentFilter = new IntentFilter(UsbManager.ActionUsbDeviceAttached);
                var intentFilter2 = new IntentFilter(UsbManager.ActionUsbDeviceDetached);
                RegisterReceiver(usbReciever, intentFilter);
                RegisterReceiver(usbReciever, intentFilter2);
                usbReciever.UsbAttachedOrDetached += UsbAttachedOrDetached;

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while initializing UsbManager");
            }

            _audioReceiver = new BackgroundWorker();
            _audioReceiver.WorkerSupportsCancellation = true;
            _audioReceiver.DoWork += _audioReceiver_DoWork;
            _audioReceiver.RunWorkerCompleted += _audioReceiver_RunWorkerCompleted;

            base.OnCreate(savedInstanceState);
        }

        private void RestartAudio()
        {
            _loggingService.Info("RestartAudio");

            if (_audioReceiver.IsBusy)
            {
                _loggingService.Info("Stopping _audioReceiver");

                _startAudioReceiverThread = true;
                _audioReceiver.CancelAsync();
            }
            else
            {
                _audioReceiver.RunWorkerAsync();
            }
        }

        private void _audioReceiver_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_startAudioReceiverThread)
            {
                _audioReceiver.RunWorkerAsync();
                _startAudioReceiverThread = false;
            }
        }

        private void _audioReceiver_DoWork(object sender, DoWorkEventArgs e)
        {
            _loggingService.Info("Starting _audioReceiver");

            var bufferSize = AudioTrack.GetMinBufferSize(_audioSampleRate, _audioChannels == 1 ? ChannelOut.Mono : ChannelOut.Stereo, Encoding.Pcm16bit);
            var _audioTrack = new AudioTrack(Android.Media.Stream.Music, _audioSampleRate, _audioChannels == 1 ? ChannelOut.Mono : ChannelOut.Stereo, Encoding.Pcm16bit, bufferSize, AudioTrackMode.Stream);

            _audioTrack.Play();

            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), _audioRecieverPort);
            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                client.Bind(remoteEP);

                var packetBuffer = new byte[UDPStreamer.MaxPacketSize];

                while (!_audioReceiver.CancellationPending)
                {
                    if (client.Available > 0)
                    {
                        var bytesRead = client.Receive(packetBuffer);

                        _audioTrack.Write(packetBuffer, 0, bytesRead);
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }

                client.Close();
            }

            _audioTrack.Stop();

            _loggingService.Info("_audioReceiver finished");
        }


        private async void UsbAttachedOrDetached(object sender, EventArgs e)
        {
            _loggingService.Info("USB Attached or detached");

            WeakReferenceMessenger.Default.Send(new USBChangedMessage(String.Empty));
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

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _loggingService.Error(e.ExceptionObject as Exception, "CurrentDomain_UnhandledException");
        }

        private void SubscribeMessages()
        {
            WeakReferenceMessenger.Default.Register<ToastMessage>(this, (r, m) =>
            {
                ShowToastMessage(m.Value);
            });


            WeakReferenceMessenger.Default.Register<NotifyAudioChangeMessage>(this, (sender, obj) =>
            {
                //if (obj.Value is AudioDataDescription desc)
                //{
                //    _audioSampleRate = desc.SampleRate;
                //    _audioChannels = desc.Channels;
                    RestartAudio();
                //}
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverConnectAndroidMessage>(this, (r, m) =>
            {
                InitDriver();
            });

            WeakReferenceMessenger.Default.Register<RTLSDRDriverConnectAndroidMessage>(this, (sender, obj) =>
            {
                if (obj.Value is DriverSettings settings)
                {
                     InitRTLSDRDriver(settings.Port, settings.Streamport, settings.SDRSampleRate);
                    //_streamPort = settings.Streamport;
                }
            });

            WeakReferenceMessenger.Default.Register<DispatchKeyEventEnabledMessage>(this, (r, m) =>
            {
                _dispatchKeyEventEnabled = m.Value;
                _loggingService.Info($"DispatchKeyEventEnabled: {_dispatchKeyEventEnabled}");
                if (m.Value)
                {
                    _dispatchKeyEventEnabledAt = DateTime.Now;
                }
            });

            WeakReferenceMessenger.Default.Register<PlayInBackgroundNotificationMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShowPlayingNotification(m.Value));
                });
            });

            WeakReferenceMessenger.Default.Register<ShowRecordNotificationMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShowRecordNotification(m.Value));
                });
            });

            WeakReferenceMessenger.Default.Register<StopRecordNotificationMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StopNotification(2);
                });
            });

            WeakReferenceMessenger.Default.Register<StopPlayInBackgroundNotificationMessage>(this, (r, m) =>
{
MainThread.BeginInvokeOnMainThread(() =>
{
    StopNotification(1);
});
});

            WeakReferenceMessenger.Default.Register<ShareFileMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Task.Run(async () => await ShareFile(m.Value));
                });
            });

            WeakReferenceMessenger.Default.Register<QuitAppMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StopNotification(1);
                    StopNotification(2);
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                });
            });

                        WeakReferenceMessenger.Default.Register<RemoteKeyPlatformActionMessage>(this, (r, m) =>
            {
                SendRemoteKey(m.Value);
            });

            WeakReferenceMessenger.Default.Register<ShowFullscreenMessage>(this, (r, m) =>
            {
                SetFullScreen();
            });
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
                var res = new KeyDownMessage(e.KeyCode.ToString());

                if (e.IsLongPress)
                {
                    res.Long = true;
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

                        WeakReferenceMessenger.Default.Send(res);

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
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, $"DispatchKeyEvent error:");
                return true;
            }
        }

        [Obsolete]
        private void SetFullScreen()
        {
            /*
            // https://stackoverflow.com/questions/39248138/how-to-hide-bottom-bar-of-android-back-home-in-xamarin-forms
            var defaultUiOptions = (int)Window.DecorView.SystemUiVisibility;

            var fullscreenUiOptions = defaultUiOptions;
            fullscreenUiOptions |= (int)SystemUiFlags.LowProfile;
            fullscreenUiOptions |= (int)SystemUiFlags.Fullscreen;
            fullscreenUiOptions |= (int)SystemUiFlags.HideNavigation;
            fullscreenUiOptions |= (int)SystemUiFlags.ImmersiveSticky;

            try
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    Window.DecorView.SystemUiVisibility = (StatusBarVisibility)fullscreenUiOptions;
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }*/
        }

        private void ShowToastMessage(string message, int AppFontSize = 0)
        {

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    _instance?.Cancel();
                    _instance = Android.Widget.Toast.MakeText(Android.App.Application.Context, message, ToastLength.Short);

                    TextView textView;
                    Snackbar snackBar = null;

                    var tView = _instance.View;
                    if (tView == null)
                    {
                        // Since Android 11, custom toast is deprecated - using snackbar instead:

                        //Activity activity = CrossCurrentActivity.Current.Activity;
                        var view = FindViewById(Android.Resource.Id.Content);

                        snackBar = Snackbar.Make(view, message, Snackbar.LengthLong);

                        textView = snackBar.View.FindViewById<TextView>(Resource.Id.snackbar_text);
                    }
                    else
                    {
                        // using Toast

                        tView.Background.SetColorFilter(Android.Graphics.Color.Gray, PorterDuff.Mode.SrcIn); //Gets the actual oval background of the Toast then sets the color filter
                        textView = (TextView)tView.FindViewById(Android.Resource.Id.Message);
                        textView.SetTypeface(Typeface.DefaultBold, TypefaceStyle.Bold);
                    }

                    var minTextSize = textView.TextSize; // 16

                    textView.SetTextColor(Android.Graphics.Color.White);

                    var screenHeightRate = 0;

                    //configuration font size:

                    //Normal = 0,
                    //AboveNormal = 1,
                    //Big = 2,
                    //Biger = 3,
                    //VeryBig = 4,
                    //Huge = 5

                    if (DeviceDisplay.MainDisplayInfo.

                    Height < DeviceDisplay.MainDisplayInfo.Width)
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

                    var fontSize = minTextSize + AppFontSize * fontSizePerValue;

                    textView.SetTextSize(Android.Util.ComplexUnitType.Px, Convert.ToSingle(fontSize));

                    if (snackBar != null)
                    {
                        snackBar.Show();
                    }
                    else
                    {
                        _instance.Show();
                    }
                }
                catch (Exception ex)
                {

                }
            });
        }

        private void InitRTLSDRDriver(int port, int streamPort, int samplerate = 2048000)
        {
            try
            {
                _loggingService.Info($"Initializing RTLSDR driver: port:{port}, sampleRate: {samplerate}");

                var req = new Intent(Intent.ActionView);
                req.SetData(Android.Net.Uri.Parse($"iqsrc://-a 127.0.0.1 -p \"{port}\" -s \"{samplerate}\""));
                //req.SetData(Android.Net.Uri.Parse($"iqsrc://-a 127.0.0.1 -p \"5658\" -s \"2048000\""));

                req.PutExtra(Intent.ExtraReturnResult, true);
                _SDRDriverStreamPort = streamPort;
                _SDRDriverPort = port;

                StartActivityForResult(req, StartRequestCodeRTLSDR);
            }
            catch (ActivityNotFoundException ex)
            {
                WeakReferenceMessenger.Default.Send(new ToastMessage("Driver not installed"));

                _loggingService.Info("Activity not found");
                WeakReferenceMessenger.Default.Send(new DVBTDriverNotInstalledMessage("Device response timeout"));
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(new ToastMessage("Driver initializing failed"));
            }
        }

        public void InitDriver()
        {
            try
            {
                var req = new Intent(Intent.ActionView);
                var scheme = new Android.Net.Uri.Builder().Scheme("dtvdriver");
                req.SetData(scheme.Build());
                req.PutExtra(Intent.ExtraReturnResult, true);

                _waitingForInit = true;

                Task.Run(async () =>
                {
                    await Task.Delay(10000); // wait 10 secs;

                    if (_waitingForInit)
                    {
                        _waitingForInit = false;

                        _loggingService.Info("Device response timeout");
                        WeakReferenceMessenger.Default.Send(new DVBTDriverConnectionFailedMessage("Device response timeout"));
                    }

                });

                _loggingService.Info("Starting activity");
                StartActivityForResult(req, StartRequestCode);

            } catch (ActivityNotFoundException e)
            {
                _loggingService.Info("Activity not found");
                WeakReferenceMessenger.Default.Send(new ToastMessage("Driver not installed"));
                WeakReferenceMessenger.Default.Send(new DVBTDriverNotInstalledMessage("Device response timeout"));
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "InitDriver");
                _waitingForInit = false;
            }
        }

        private static string GetAndroidDirectory(string specialFolder)
        {
            try
            {
                if (specialFolder == null)
                {
                    // internal storage - always writable directory
                    try
                    {
                        var pathToExternalMediaDirs = Android.App.Application.Context.GetExternalMediaDirs();

                        if (pathToExternalMediaDirs.Length == 0)
                            throw new DirectoryNotFoundException("No external media directory found");

                        return pathToExternalMediaDirs[0].AbsolutePath;
                    }
                    catch
                    {
                        // fallback for older API:

                        var internalStorageDir = Android.App.Application.Context.GetExternalFilesDir(Environment.SpecialFolder.MyDocuments.ToString());

                        return internalStorageDir.AbsolutePath;
                    }
                }
                else
                {
                    // external storage

                    // Android 11 has no access to external files -> using internal storage
                    if (Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.P)
                    {
                        var internalStorageDir = Android.App.Application.Context.GetExternalFilesDir(Environment.SpecialFolder.MyDocuments.ToString());
                        return internalStorageDir.AbsolutePath;
                    }
                    else
                    {

                        var externalFolderPath = Android.OS.Environment.GetExternalStoragePublicDirectory(specialFolder);
                        return externalFolderPath.AbsolutePath;
                    }
                }

            }
            catch
            {
                var dir = Android.App.Application.Context.GetExternalFilesDir("");

                return dir.AbsolutePath;
            }
        }

        private void ProcessDriverConnectResult(Result resultCode, Intent data)
        {
            _waitingForInit = false;

            if (resultCode == Result.Canceled)
            {
                return;
            }

            if (resultCode == Result.Ok)
            {
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

                    cfg.PublicDirectory = GetAndroidDirectory(null);

                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectedMessage(cfg));
                }
                else
                {
                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectionFailedMessage("Bad activity result"));
                }
            }
            else
            {
                var errorCodeString = "Bad activity result";

                if (data != null && data.Extras != null && !data.Extras.IsEmpty)
                {
                    if (data.HasExtra("ErrorCode"))
                        errorCodeString = data.GetStringExtra("ErrorCode");

                    foreach (var key in data.Extras.KeySet())
                    {
                        var value = data.Extras.Get(key);
                        _loggingService.Info($"Key: {key}, Value: {value}");
                    }
                }
                WeakReferenceMessenger.Default.Send(new DVBTDriverConnectionFailedMessage(errorCodeString));
            }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == StartRequestCode)
            {
                ProcessDriverConnectResult(resultCode,data);
            }
            if (requestCode == StartRequestCodeRTLSDR)
            {
                if (resultCode == Result.Ok)
                {
                    var x = data.GetIntExtra("SDRDriverPort", 1234);
                    var y = data.GetIntExtra("SDRDriverStreamPort", 1235);

                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectedMessage(new DVBTDriverConfiguration()
                    {
                        //SupportedTcpCommands = data.GetIntArrayExtra("supportedTcpCommands"),
                        DeviceName = data.GetStringExtra("deviceName"),
                        ControlPort = _SDRDriverPort,
                        TransferPort = _SDRDriverStreamPort,
                        PublicDirectory = GetAndroidDirectory(null)
                    }));

                    //+RestartAudio();
                }
                else
                {
                    var errorMsg = (data == null ? "no description" : data.GetStringExtra("detailed_exception_message"));

                    _loggingService.Info($"Driver Init failed: {errorMsg}");

                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectionFailedMessage(errorMsg));/*(""));DVBTDriverConfiguration()
                    {
                        ErrorId = data == null ? -1 : data.GetIntExtra("marto.rtl_tcp_andro.RtlTcpExceptionId", -1),
                        ExceptionCode = data == null ? 0 : data.GetIntExtra("detailed_exception_code", 0),
                        DetailedDescription = data == null ? "unknown" : data.GetStringExtra("detailed_exception_message")
                    }));*/
                }
            }
        }
    }
}
