using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xamarin.Forms;

namespace DVBTTelevizor
{
    public class BackgroundCommandWorker
    {
        /// <summary>
        /// Running command in background
        /// </summary>
        /// <param name="command"></param>
        /// <param name="repeatIntervalSeconds">0 and negative for no repeat</param>
        /// <param name="delaySeconds">start delay</param>
        public static void RunInBackground(Command command, int repeatIntervalSeconds = 5, int delaySeconds = 0)
        {           
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                Thread.Sleep(delaySeconds * 1000);

                do
                {
                    Xamarin.Forms.Device.BeginInvokeOnMainThread(new Action(delegate { command.Execute(null); }));

                    if (repeatIntervalSeconds <= 0)
                    {
                        break;
                    }
                    else
                    {
                        Thread.Sleep(repeatIntervalSeconds * 1000);
                    }
                } while (true);
            }).Start();         
        }
    }
}
