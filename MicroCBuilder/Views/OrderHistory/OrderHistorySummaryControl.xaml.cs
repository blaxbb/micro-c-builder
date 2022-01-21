using MicroCBuilder.Models;
using MicroCBuilder.ViewModels;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
            if(ctx is OrderHistorySummaryItem item)
            {
                MainPage.Instance.CreateOrderHistory(item.Name);
            }
        }
    }
}
