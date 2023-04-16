using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;

namespace DVBTTelevizor.Droid
{
    [Activity(Name= "net.petrjanousek.DVBTTelevizor.SplashActivity", Label = "Starting DVBT Televizor", Theme = "@style/SplashTheme", MainLauncher = true, NoHistory = true, Exported = true)]
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