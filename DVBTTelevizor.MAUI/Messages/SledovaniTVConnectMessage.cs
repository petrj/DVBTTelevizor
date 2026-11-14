using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    public class SledovaniTVConnectMessage : ValueChangedMessage<bool>
    {
        public SledovaniTVConnectMessage(bool value) : base(value)
        {
        }
    }
}
