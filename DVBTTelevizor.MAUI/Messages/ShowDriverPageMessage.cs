using CommunityToolkit.Mvvm.Messaging.Messages;
using DVBTTelevizor.TV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVBTTelevizor.MAUI.Messages
{
    internal class ShowDriverPageMessage : ValueChangedMessage<AppDriverTypeEnum>
    {
        public ShowDriverPageMessage(AppDriverTypeEnum value) : base(value)
        {

        }
    }

    internal class ShowTuningProgressDriverPageMessage : ShowDriverPageMessage
    {
        public ShowTuningProgressDriverPageMessage(AppDriverTypeEnum value) : base(value)
        {
        }
    }

    internal class ShowSelectDriverDriverPageMessage : ShowDriverPageMessage
    {
        public ShowSelectDriverDriverPageMessage(AppDriverTypeEnum value) : base(value)
        {
        }
    }

    internal class ShowTuneDriverPageMessage : ShowDriverPageMessage
    {
        public ShowTuneDriverPageMessage(AppDriverTypeEnum value) : base(value)
        {
        }
    }
}
