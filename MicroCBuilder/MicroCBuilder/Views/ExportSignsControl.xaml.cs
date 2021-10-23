using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MicroCBuilder.Views
{
    public sealed partial class ExportSignsControl : UserControl
    {
        public ExportSignsControl()
        {
            this.InitializeComponent();
            UsernameTextBox.Text = Settings.SignUsername();
            PasswordTextBox.Password = Settings.SignPassword();
            SavePasswordCheckBox.IsChecked = !string.IsNullOrWhiteSpace(PasswordTextBox.Password);
            BaseUrlTextBox.Text = Settings.SignBaseUrl();
        }

        public ExportSignsControl(string? title)
            : this()
        {
            TitleTextBox.Text = title ?? "";
        }

        public string Title => TitleTextBox.Text;
        public string SignType => SignTypeComboxBox.SelectedItem.ToString();
        public string Username => UsernameTextBox.Text;
        public string Password => PasswordTextBox.Password;
        public string BaseUrl => BaseUrlTextBox.Text;
        public bool SavePassword => SavePasswordCheckBox.IsChecked ?? false;
        public bool UseQuantity => UseQuantityCheckbox.IsChecked ?? false;
    }
}
