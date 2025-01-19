using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class StopRecordMessage : ValueChangedMessage<string?>
    {
        public StopRecordMessage(string value) : base(value)
        {

        }
    }
}
