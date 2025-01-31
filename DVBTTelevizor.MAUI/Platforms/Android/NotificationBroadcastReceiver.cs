using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CommunityToolkit.Mvvm.Messaging;
using DVBTTelevizor.MAUI.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVBTTelevizor.MAUI
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
                    WeakReferenceMessenger.Default.Send(new StopStreamMessage(null));
                }
                if (intent.Action == "Quit")
                {
                    WeakReferenceMessenger.Default.Send(new QuitAppMessage(null));
                }
                if (intent.Action == "StopRecord")
                {
                    WeakReferenceMessenger.Default.Send(new StopRecordMessage(null));
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}