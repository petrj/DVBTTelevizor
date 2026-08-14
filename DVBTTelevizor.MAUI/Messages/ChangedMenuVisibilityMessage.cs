using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class ChangedMenuVisibilityMessage : ValueChangedMessage<bool>
    {
        public ChangedMenuVisibilityMessage(bool value) : base(value)
        {

        }
    }
}
