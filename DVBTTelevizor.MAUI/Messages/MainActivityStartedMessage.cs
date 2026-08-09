using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class MainActivityStartedMessage : ValueChangedMessage<string?>
    {
        public MainActivityStartedMessage(string value) : base(value)
        {

        }
    }
}
