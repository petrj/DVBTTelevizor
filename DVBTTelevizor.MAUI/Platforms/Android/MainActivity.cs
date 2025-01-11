using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Widget;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using Google.Android.Material.Snackbar;
using LoggerService;
using NLog;
using System.Reflection;
using Environment = System.Environment;

namespace DVBTTelevizor.MAUI
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private const int StartRequestCode = 1000;
        private bool _waitingForInit = false;
        private static Android.Widget.Toast _instance;
        private ILoggingService _loggingService = null;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Assembly assembly = typeof(App).GetTypeInfo().Assembly;
            NLog.Config.ISetupBuilder setupBuilder = NLog.LogManager.Setup();
            NLog.Config.ISetupBuilder configuredSetupBuilder = setupBuilder.LoadConfigurationFromAssemblyResource(assembly);
            _loggingService = new NLogLoggingService(configuredSetupBuilder.GetCurrentClassLogger());

            SubscribeMessages();

            var dir = GetAndroidDirectory(null);

            base.OnCreate(savedInstanceState);
        }

        private void SubscribeMessages()
        {
            WeakReferenceMessenger.Default.Register<ToastMessage>(this, (r, m) =>
            {
                ShowToastMessage(m.Value);
            });

            WeakReferenceMessenger.Default.Register<DVBTDriverConnectMessage>(this, (r, m) =>
            {
                InitDriver();
            });
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

        public void InitDriver()
        {
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
                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectionFailedMessage("Device response timeout"));
                }

            });

            _loggingService.Info("Starting activity");
            StartActivityForResult(req, StartRequestCode);
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


        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == StartRequestCode)
            {
                if (resultCode == Result.Ok)
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

                        WeakReferenceMessenger.Default.Send(new DVBTDriverConnectedMessage(cfg));
                    }
                    else
                    {
                        WeakReferenceMessenger.Default.Send(new DVBTDriverConnectionFailedMessage("Bad activity result"));
                    }
                } else
                {
                    if (data.Extras != null && !data.Extras.IsEmpty)
                    {
                        foreach (var key in data.Extras.KeySet())
                        {
                            var value = data.Extras.Get(key);
                            _loggingService.Info($"Key: {key}, Value: {value}");
                        }
                    }
                    WeakReferenceMessenger.Default.Send(new DVBTDriverConnectionFailedMessage("Bad activity result"));
                }
            }
        }


    }
}
