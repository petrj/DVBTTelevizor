using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVBTTelevizor.MAUI
{
    public class StatValueTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is StatValue stat && stat.Value is bool)
                return BoolTemplate;

            return TextTemplate;
        }
    }
}
