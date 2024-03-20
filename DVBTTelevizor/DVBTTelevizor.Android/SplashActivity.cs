using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;
using Android.Webkit;

namespace DVBTTelevizor.Droid
{
    [Activity(Name= "net.petrjanousek.DVBTTelevizor.SplashActivity", Label = "DVBT Televizor", Theme = "@style/SplashTheme", MainLauncher = true, Icon = "@drawable/icon", Banner = "@drawable/banner", NoHistory = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionMain }, AutoVerify = true, Categories = new[] { Intent.CategoryLeanbackLauncher })]
    public class SplashActivity : AppCompatActivity
    {
        public override void OnCreate(Bundle savedInstanceState, PersistableBundle persistentState)
        {
            base.OnCreate(savedInstanceState, persistentState);
        }

        protected override void OnResume()
        {
            base.OnResume();
            new Task(() => { StartMainActivity(); }).Start();
        }

        // Prevent the back button from canceling the startup process
        public override void OnBackPressed() { }

        // Simulates background work that happens behind the splash screen
        private async void StartMainActivity ()
        {
            StartActivity(new Intent(Application.Context, typeof (MainActivity)));
        }
    }
}