using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    public class SizedToast
    {
        public string? Message { get; set; }
        public AppFontSizeEnum AppFontSize { get; set; } = AppFontSizeEnum.Normal;
    }

    public class ToastMessage : ValueChangedMessage<string>
    {
        public ToastMessage(string value) : base(value)
        {

        }
    }

    public class SizedToastMessage : ValueChangedMessage<SizedToast>
    {
        public SizedToastMessage(SizedToast value) : base(value)
        {

        }
    }
}
