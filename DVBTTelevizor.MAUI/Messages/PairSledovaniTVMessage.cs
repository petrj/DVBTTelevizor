using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    public class PairSledovaniTVMessage : ValueChangedMessage<string>
    {
        public PairSledovaniTVMessage(string value) : base(value)
        {
        }
    }
}
