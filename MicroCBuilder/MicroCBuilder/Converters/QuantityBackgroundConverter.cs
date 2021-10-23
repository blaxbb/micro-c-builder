using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MicroCBuilder.Converters
{
    public class QuantityBackgroundConverter : IValueConverter
    {
        public string? Format { get; set; }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is int i && i > 1)
            {
                return new SolidColorBrush(Microsoft.UI.Colors.LightGray);
            }
            return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
