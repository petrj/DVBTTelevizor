using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class ChannelsChangedMessage : ValueChangedMessage<string>
    {
        public ChannelsChangedMessage(string value) : base(value)
        {

        }
    }
}
