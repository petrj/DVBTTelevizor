using CommunityToolkit.Mvvm.Messaging.Messages;
using DVBTTelevizor.TV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class SendConnectDriverRequestMessage : ValueChangedMessage<AppDriverTypeEnum>
    {
        public SendConnectDriverRequestMessage(AppDriverTypeEnum value) : base(value)
        {

        }
    }
}
