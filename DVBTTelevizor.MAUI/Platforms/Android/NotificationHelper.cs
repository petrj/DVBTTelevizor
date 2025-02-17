﻿using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Application = Android.App.Application;

namespace DVBTTelevizor.MAUI
{
    // https://github.com/xamarin/monodroid-samples
    public class NotificationHelper : ContextWrapper
    {
        public const string _channelId = "default";
        public const int _notificationId = 1;
        public const int noti_channel_default = 2131165200;
        NotificationManager _notificationManager;

        private NotificationManager NotificationManager
        {
            get
            {
                if (_notificationManager == null)
                {
                    _notificationManager = (NotificationManager)GetSystemService(NotificationService);
                }
                return _notificationManager;
            }
        }

        int SmallIcon => Android.Resource.Drawable.StatNotifyChat;

        public NotificationHelper(Context context) : base(context)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                // Notification channels are new in API 26 (and not a part of the
                // support library). There is no need to create a notification
                // channel on older versions of Android.

                var channel = new NotificationChannel(_channelId, GetString(noti_channel_default), NotificationImportance.Low);
                channel.LockscreenVisibility = NotificationVisibility.Public;
                channel.SetVibrationPattern(new long[] { 0, 0 });
                channel.SetSound(null, null);
                NotificationManager.CreateNotificationChannel(channel);
            }
        }

        public void ShowPlayNotification(int notificationId, string title, string body, string detail)
        {
            var notificationIntent = Application.Context.PackageManager?.GetLaunchIntentForPackage(Application.Context.PackageName);
            notificationIntent.SetFlags(ActivityFlags.SingleTop);
            var pendingIntentFlags = PendingIntentFlags.CancelCurrent | PendingIntentFlags.Immutable;
            var pendingIntent = PendingIntent.GetActivity(Application.Context, notificationId, notificationIntent, pendingIntentFlags);

            var stopIntent = new Intent(Application.Context, typeof(NotificationBroadcastReceiver));
            stopIntent.SetAction("Stop");

            var stopPendingIntent = PendingIntent.GetBroadcast(
                Application.Context,
                notificationId,
                stopIntent,
                pendingIntentFlags
            );

            var quitIntent = new Intent(Application.Context, typeof(NotificationBroadcastReceiver));
            quitIntent.SetAction("Quit");
            var quitPendingIntent = PendingIntent.GetBroadcast(
                Application.Context,
                notificationId,
                quitIntent,
                pendingIntentFlags
            );

            var notificationBuilder = new NotificationCompat.Builder(ApplicationContext, _channelId)
                     .SetContentTitle(body)
                     .SetContentText(detail)
                     .SetSubText(title)
                     .SetSmallIcon(Resource.Drawable.SmallIcon)
                     .SetAutoCancel(false)
                     .SetOngoing(true)
                     .SetSound(null)
                     .SetVibrate(new long[] { 0, 0 })
                     .AddAction(new NotificationCompat.Action(Resource.Drawable.Stop, "Stop", stopPendingIntent))
                     .AddAction(new NotificationCompat.Action(Resource.Drawable.Quit, "Quit", quitPendingIntent))
                     .SetVisibility((int)NotificationVisibility.Public)
                     .SetContentIntent(pendingIntent);

            NotificationManager.Notify(notificationId, notificationBuilder.Build());
        }

        public void ShowRecordNotification(int notificationId, string title, string body, string detail)
        {
            var notificationIntent = Application.Context.PackageManager?.GetLaunchIntentForPackage(Application.Context.PackageName);
            notificationIntent.SetFlags(ActivityFlags.SingleTop);
            var pendingIntentFlags = PendingIntentFlags.CancelCurrent | PendingIntentFlags.Immutable;
            var pendingIntent = PendingIntent.GetActivity(Application.Context, notificationId, notificationIntent, pendingIntentFlags);

            var stopIntent = new Intent(Application.Context, typeof(NotificationBroadcastReceiver));
            stopIntent.SetAction("StopRecord");

            var stopPendingIntent = PendingIntent.GetBroadcast(
                Application.Context,
                notificationId,
                stopIntent,
                pendingIntentFlags
            );

            var notificationBuilder = new NotificationCompat.Builder(ApplicationContext, _channelId)
                     .SetContentTitle(body)
                     .SetContentText(detail)
                     .SetSubText(title)
                     .SetSmallIcon(Resource.Drawable.SmallIcon)
                     .SetAutoCancel(false)
                     .SetOngoing(true)
                     .SetSound(null)
                     .SetVibrate(new long[] { 0, 0 })
                     .AddAction(new NotificationCompat.Action(Resource.Drawable.Stop, "Stop recording", stopPendingIntent))
                     .SetVisibility((int)NotificationVisibility.Public)
                     .SetContentIntent(pendingIntent);

            NotificationManager.Notify(notificationId, notificationBuilder.Build());
        }

        public void CloseNotification(int notificationId)
        {
            NotificationManager.Cancel(notificationId);
        }
    }
}