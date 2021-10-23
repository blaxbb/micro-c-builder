using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MicroCBuilder.Converters
{
    public class NullVisibilityConverter : IValueConverter
    {
        public bool NullIsCollapsed { get; set; } = true;
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is string s)
            {
                if(string.IsNullOrWhiteSpace(s))
                {
                    return NullIsCollapsed ? Visibility.Collapsed : Visibility.Visible;
                }

                return NullIsCollapsed ? Visibility.Visible : Visibility.Collapsed;
            }
            if(value is bool b)
            {
                //true  -> true -> visible
                //true  -> false   -> collapsed
                //false -> true -> collapsed
                //false -> false   -> visible
                return b == NullIsCollapsed ? Visibility.Visible : Visibility.Collapsed;
            }
            if(value is IList list)
            {
                return (list == null || list.Count == 0) == NullIsCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }

            if(value is null)
            {
                return  NullIsCollapsed ? Visibility.Collapsed : Visibility.Visible;
            }

            return NullIsCollapsed ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
