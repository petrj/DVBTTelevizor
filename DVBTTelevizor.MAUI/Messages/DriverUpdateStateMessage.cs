using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    public class DriverState
    {
        public string BitRate { get; set; }
        public string Frequency { get; set; }
    }

    public class DriverUpdateStateMessage : ValueChangedMessage<DriverState?>
    {
        public DriverUpdateStateMessage(DriverState? value) : base(value)
        {

        }
    }
}
