using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor.Droid
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "net.petrjanousek.net.NotificationBroadcastReceiver" })]
    public class NotificationBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            try
            {
                if (intent.Action == "Stop")
                {
                    MessagingCenter.Send("", BaseViewModel.MSG_StopStream);
                }
                if (intent.Action == "Quit")
                {
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}