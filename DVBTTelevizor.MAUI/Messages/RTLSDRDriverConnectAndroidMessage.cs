using CommunityToolkit.Mvvm.Messaging.Messages;
using RTLSDR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    public class RTLSDRDriverConnectAndroidMessage : ValueChangedMessage<DriverSettings>
    {
        public RTLSDRDriverConnectAndroidMessage(DriverSettings value) : base(value)
        {

        }
    }
}
