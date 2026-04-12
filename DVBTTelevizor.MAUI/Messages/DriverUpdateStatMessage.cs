using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTLSDR.Common;

namespace DVBTTelevizor.MAUI.Messages
{
    public class DriverStat
    {
        public string? BitRate { get; set; }
        public string? Frequency { get; set; }

        public string? Stat { get; set; }
        public List<StatValue>? StatValues { get; set; }
    }

    public class DriverUpdateStatMessage : ValueChangedMessage<DriverStat?>
    {
        public DriverUpdateStatMessage(DriverStat? value) : base(value)
        {

        }
    }
}
