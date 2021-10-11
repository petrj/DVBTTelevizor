using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using Plugin.CurrentActivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Forms;

namespace DVBTTelevizor.Droid
{
    // https://stackoverflow.com/questions/59210927/get-attached-usb-device-information-in-xamarin-android

    public class BroacastReceiverSystem : BroadcastReceiver
    {
        public BroacastReceiverSystem() { }
        public event EventHandler UsbAttached;
        public override void OnReceive(Context c, Intent i)
        {
            UsbAttached(this, EventArgs.Empty);
        }
    }
}