using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace MicroCBuilder.Converters
{
    public class ZeroAsEmptyStringConverter : IValueConverter
    {
        public string? Format { get; set; }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            switch (value)
            {
                case float f when f == 0:
                    return string.Empty;
                case double d when d == 0:
                    return string.Empty;
                case int i when i == 0:
                    return string.Empty;
                default:
                    return Format == null ? value : string.Format(Format, value);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
