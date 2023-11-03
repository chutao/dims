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
    public class LogLevelToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            ISolidColorBrush? brush = null;
            if (value is Splat.LogLevel level)
            {
                switch (level)
                {
                    case Splat.LogLevel.Debug:
                        brush = Brushes.Pink;
                        break;
                    case Splat.LogLevel.Info:
                        brush = Brushes.Black;
                        break;
                    case Splat.LogLevel.Warn:
                        brush = Brushes.Yellow;
                        break;
                    case Splat.LogLevel.Error:
                        brush = Brushes.Red;
                        break;
                    case Splat.LogLevel.Fatal:
                        brush = Brushes.DarkRed;
                        break;
                    default:
                        break;
                }
            }

            return brush;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
