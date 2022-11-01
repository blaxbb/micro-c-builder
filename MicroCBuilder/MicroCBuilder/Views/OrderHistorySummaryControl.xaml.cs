using MicroCBuilder.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MicroCBuilder.Views
{
    public sealed partial class OrderHistorySummaryControl : UserControl
    {
        public OrderHistorySummaryControl()
        {
            this.InitializeComponent();
        }


        private void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var ctx = ((Grid)sender).DataContext;
            if (ctx is OrderHistorySummaryItem item)
            {
                MainPage.Instance.CreateOrderHistory(item.Name);
            }
        }
    }
}
