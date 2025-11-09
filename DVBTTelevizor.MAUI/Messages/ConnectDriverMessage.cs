using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    public class ConnectDriverMessage : DVBTDriverConnectMessages<DVBTDriverConfiguration>
    {
        public ConnectDriverMessage(DVBTDriverConfiguration value) : base(value)
        {

        }
    }
}
