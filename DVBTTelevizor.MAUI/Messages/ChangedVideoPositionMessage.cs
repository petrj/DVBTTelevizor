using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class ChangedVideoPositionMessage : ValueChangedMessage<Rect?>
    {
        public ChangedVideoPositionMessage(Rect value) : base(value)
        {

        }
    }
}
