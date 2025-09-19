using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class TuneFailedMessage : ValueChangedMessage<string>
    {
        public TuneFailedMessage(string value) : base(value)
        {

        }
    }
}
