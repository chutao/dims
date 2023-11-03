using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Helpers
{
    public class BooleanToColorConverter : IValueConverter
    {
        public static BooleanToColorConverter Instance { get; } = new BooleanToColorConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool? @bool = (bool?)value;
            if (!@bool.HasValue)
                return Colors.Gray;
            else if (@bool == true)
                return Colors.Green;
            else
                return Colors.Red;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
