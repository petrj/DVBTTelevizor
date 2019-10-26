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



namespace DVBTTelevizor.Droid
{
    [Activity(Label = "DVBTTelevizor", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private const int StartRequestCode = 1000;
        bool _waitingForInit = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;            

            base.OnCreate(savedInstanceState);

            // workaround pro ne-pouziti FileProvideru:
            // https://stackoverflow.com/questions/38200282/android-os-fileuriexposedexception-file-storage-emulated-0-test-txt-exposed
            StrictMode.VmPolicy.Builder builder = new StrictMode.VmPolicy.Builder();
            StrictMode.SetVmPolicy(builder.Build());

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            var app = new App();
            LoadApplication(app);

            MessagingCenter.Subscribe<string>(this, "Init", (message) =>
            {
                try
                {
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
                            MessagingCenter.Send("Driver response timeout", "DVBTDriverConfigurationFailed");
                        }

                    });

                    StartActivityForResult(req, StartRequestCode);

                } catch (Exception ex)
                {
                    _waitingForInit = false;
                    MessagingCenter.Send(ex.ToString(), "DVBTDriverConfigurationFailed");
                }
            });

            MessagingCenter.Subscribe<string>(this, "PlayUrl", (url) =>
            {
                var intent = new Intent(Intent.ActionView);
                var uri = Android.Net.Uri.Parse(url);
                intent.SetDataAndType(uri, "video/*");
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask); // necessary for Android 5
                Android.App.Application.Context.StartActivity(intent);
            });

            MessagingCenter.Subscribe<string>(this, "PlayStream", (name) =>
            {
                Core.Initialize();

                var _libVLC = new LibVLC();
                var _mediaPlayer = new MediaPlayer(_libVLC) { EnableHardwareDecoding = true };

                var _videoView = new VideoView(this) { MediaPlayer = _mediaPlayer };
                AddContentView(_videoView, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
                //var media = new Media(_libVLC, "https://www.w6rz.net/newmobcal1920_12mbps.ts", FromType.FromLocation);
                //var media = new Media(_libVLC, "/storage/emulated/0/Download/stream.ts", FromType.FromPath);
                var media = new Media(_libVLC, app.VideoStream, new string[] { } );

                _videoView.MediaPlayer.Play(media); 
            });

            // wifi state permission required
            //WifiManager wifiManager = (WifiManager)Android.App.Application.Context.GetSystemService(Service.WifiService);
            //int ip = wifiManager.ConnectionInfo.IpAddress;

                 
            
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
                       
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
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

                    MessagingCenter.Send(cfg.ToString(), "DVBTDriverConfiguration");
                } else
                {
                    MessagingCenter.Send("No response from driver", "DVBTDriverConfigurationFailed");
                }
            }
        }
    }
}