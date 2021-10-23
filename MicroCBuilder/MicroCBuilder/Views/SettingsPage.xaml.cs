using MicroCBuilder.ViewModels;
using QRCoder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MicroCBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        private WriteableBitmap bitmap;

        public WriteableBitmap Bitmap { get => bitmap; set { bitmap = value; OnPropertyChanged(nameof(Bitmap)); } }

        public SettingsPage()
        {
            DataContextChanged += SettingsPage_DataContextChanged;
            this.InitializeComponent();
        }

        private void SettingsPage_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if(DataContext is SettingsPageViewModel vm)
            {
                vm.PropertyChanged += Vm_PropertyChanged;
                CreateQrCode(vm.SharedPassword);
            }
        }

        private async void Vm_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataContext is SettingsPageViewModel vm)
            {
                if (e.PropertyName == nameof(SettingsPageViewModel.SharedPassword))
                {
                    await CreateQrCode(vm.SharedPassword);
                }
            }
        }

        public async Task CreateQrCode(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var qrGen = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(text, QRCodeGenerator.ECCLevel.H);
            var code = new BitmapByteQRCode(qrData);
            var graphic = code.GetGraphic(20);
            using (var stream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(graphic);
                    await writer.StoreAsync();
                }
                Bitmap = new WriteableBitmap(1024, 1024);
                await Bitmap.SetSourceAsync(stream);
                qrImage.Source = Bitmap;
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
