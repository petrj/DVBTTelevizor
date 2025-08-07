using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class SetAudioTrackMessage : ValueChangedMessage<string>
    {
        public SetAudioTrackMessage(string value) : base(value)
        {

        }
    }
}
