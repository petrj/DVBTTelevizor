using Android.Content;

namespace DVBTTelevizor.MAUI
{
    // https://stackoverflow.com/questions/59210927/get-attached-usb-device-information-in-xamarin-android

    public class USBBroadcastReceiverSystem : BroadcastReceiver
    {
        public USBBroadcastReceiverSystem() { }
        public event EventHandler UsbAttachedOrDetached;
        public override void OnReceive(Context c, Intent i)
        {
            UsbAttachedOrDetached(this, EventArgs.Empty);
        }
    }
}