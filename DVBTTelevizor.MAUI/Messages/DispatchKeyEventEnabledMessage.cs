using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class DispatchKeyEventEnabledMessage : ValueChangedMessage<bool>
    {
        public DispatchKeyEventEnabledMessage(bool value) : base(value)
        {

        }
    }
}
