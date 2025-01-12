using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    public class DVBTDriverNotInstalledMessage : ValueChangedMessage<string>
    {
        public DVBTDriverNotInstalledMessage(string value) : base(value)
        {

        }
    }
}
