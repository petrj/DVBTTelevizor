using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class ShowTuneMessage : ValueChangedMessage<string>
    {
        public ShowTuneMessage(string value) : base(value)
        {

        }
    }
}
