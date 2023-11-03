using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DIMS.Helpers
{
    public class LogLevelToIconConverter : IValueConverter
    {
        public static readonly LogLevelToIconConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Splat.LogLevel level)
            {
                string icon = "";
                switch (level)
                {
                    case Splat.LogLevel.Debug:
                        icon = "fa-solid fa-bug";
                        break;
                    case Splat.LogLevel.Info:
                        icon = "fa-solid fa-circle-info";
                        break;
                    case Splat.LogLevel.Warn:
                        icon = "fa-solid fa-triangle-exclamation";
                        break;
                    case Splat.LogLevel.Error:
                        icon = "fa-solid fa-bomb";
                        break;
                    case Splat.LogLevel.Fatal:
                        icon = "fa-solid fa-skull-crossbones";
                        break;
                    default:
                        break;
                }

                return icon;
            }
            else
            {
                return null;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
