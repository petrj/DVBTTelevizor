using System.Globalization;
using Microsoft.Maui.Controls;

namespace DVBTTelevizor.MAUI.Converters
{
    public class SelectionVisibilityConverter : IMultiValueConverter
    {
        public object? Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
        {
            // values[0] = current item in template
            // values[1] = selected item from binding

            if (values.Length < 2 || values[0] == null || values[1] == null)
                return true;  // Show border by default

            // If current item equals selected item, hide border
            if (values[0].Equals(values[1]))
                return false;  // Hide border for selected item

            return true;  // Show border for unselected items
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
