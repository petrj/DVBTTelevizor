using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class ExternalDeviceWriteAccessGrantedSettings
    {
        public string Path { get; set; }
        public string PathUri { get; set; }
    }

    internal class ExternalDeviceWriteAccessGranted : ValueChangedMessage<ExternalDeviceWriteAccessGrantedSettings>
    {
        public ExternalDeviceWriteAccessGranted(ExternalDeviceWriteAccessGrantedSettings value) : base(value)
        {

        }
    }
}
