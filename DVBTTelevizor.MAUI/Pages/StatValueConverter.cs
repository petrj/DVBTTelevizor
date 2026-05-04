using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DVBTTelevizor.MAUI
{
    public class StatValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "";

            // Handle double / float → round to 2 decimals
            if (value is double d)
                return d.ToString("F2", culture);

            if (value is float f)
                return f.ToString("F2", culture);

            // Everything else
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
