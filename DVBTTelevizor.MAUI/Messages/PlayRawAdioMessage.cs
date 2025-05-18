using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    public class PlayRawAdioMessage : ValueChangedMessage<string>
    {
        public PlayRawAdioMessage(string value) : base(value)
        {

        }
    }
}
