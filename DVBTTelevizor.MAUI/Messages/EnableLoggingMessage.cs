using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class EnableLoggingMessage : ValueChangedMessage<string>
    {
        public EnableLoggingMessage(string value) : base(value)
        {

        }
    }
}
