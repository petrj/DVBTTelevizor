using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class ShowDriverPageMessage : ValueChangedMessage<DriverTypeEnum>
    {
        public ShowDriverPageMessage(DriverTypeEnum value) : base(value)
        {

        }
    }
}
