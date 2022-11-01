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
    public sealed partial class MessageBox : ContentDialog
    {
        public MessageBox(XamlRoot xaml, string title, string content, string closeText = "Okay")
        {
            this.Title = title;
            this.Content = content;
            this.XamlRoot = xaml;
            this.CloseButtonText = closeText;

            this.InitializeComponent();
        }
    }
}
