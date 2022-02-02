//using LiveChartsCore;
//using LiveChartsCore.Kernel.Sketches;
//using LiveChartsCore.SkiaSharpView;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MicroCBuilder.Views
{
    public sealed partial class OrderHistoryPage : Page
    {
        public OrderHistoryPage()
        {
            this.InitializeComponent();

            //Task.Run(async () => { await InitChart(); });
            //chart.Series = new ISeries[]
            //{
            //    new LineSeries<double>
            //    {
            //        Values = new double[] { 2, 1, 3, 5, 3, 4, 6 },
            //        Fill = null
            //    }
            //};

            //chart.XAxes = new ICartesianAxis[]
            //{
            //    new Axis()
            //    {
            //        Labels = new List<string>() {"a", "b", "c", "d", "e", "f", "g"}
            //    }
            //};


        }
    }
}
