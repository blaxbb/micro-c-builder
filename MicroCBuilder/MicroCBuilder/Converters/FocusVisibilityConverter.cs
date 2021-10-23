using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace MicroCBuilder.Converters
{
    public class FocusVisibilityConverter : IValueConverter
    {
        public bool ShowWhenFocused { get; set; } = true;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is FocusState focusState)
            {
                switch (focusState)
                {
                    default:
                        return ShowWhenFocused ? Visibility.Visible : Visibility.Collapsed;
                    case FocusState.Unfocused:
                        return !ShowWhenFocused ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            return Visibility.Collapsed; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
