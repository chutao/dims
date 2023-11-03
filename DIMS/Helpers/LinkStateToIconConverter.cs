using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Helpers
{
    public class LinkStateToIconConverter : IValueConverter
    {
        public static LinkStateToIconConverter Instance { get; } = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool? @bool = (bool?)value;
            if (@bool == true)
                return "fa-solid fa-link";
            else
                return "fa-solid fa-link-slash";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
