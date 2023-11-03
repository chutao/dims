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
    public class BooleanToBrushConverter : IValueConverter
    {
        public static BooleanToBrushConverter Instance { get; } = new BooleanToBrushConverter();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool? @bool = (bool?)value;
            if (!@bool.HasValue)
                return Brushes.Gray;
            else if (@bool == true)
                return Brushes.Green;
            else
                return Brushes.Red;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
