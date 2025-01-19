using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class KeyDownMessage : ValueChangedMessage<string>
    {
        public bool Long { get; set; } = false;

        public KeyDownMessage(string value) : base(value)
        {

        }
    }
}
