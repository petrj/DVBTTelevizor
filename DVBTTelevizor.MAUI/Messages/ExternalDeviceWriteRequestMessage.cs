using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class ExternalDeviceWriteRequestMessage : ValueChangedMessage<string>
    {
        public ExternalDeviceWriteRequestMessage(string value) : base(value)
        {

        }
    }
}
