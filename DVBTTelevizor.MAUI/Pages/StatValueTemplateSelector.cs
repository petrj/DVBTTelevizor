using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DVBTTelevizor.MAUI
{
    public class StatValueTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate TableTemplate { get; set; }

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            if (item is StatValue stat)
            {
                if (stat.Value is bool)
                {
                    return BoolTemplate;
                }

                if (stat.Value is DataTable)
                {
                    return TableTemplate;
                }
            }

            return TextTemplate;
        }
    }
}
