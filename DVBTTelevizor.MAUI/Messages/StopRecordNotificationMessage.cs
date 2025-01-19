using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class StopRecordNotificationMessage : ValueChangedMessage<PlayStreamInfo>
    {
        public StopRecordNotificationMessage(PlayStreamInfo value) : base(value)
        {

        }
    }
}
