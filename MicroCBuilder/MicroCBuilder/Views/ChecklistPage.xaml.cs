using MicroCBuilder.Models;
using MicroCBuilder.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MicroCBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChecklistPage : Page
    {
        public ChecklistPage()
        {
            this.InitializeComponent();
            ChecklistListView.DragItemsCompleted += ChecklistListView_DragItemsCompleted;
        }

        private async void ChecklistListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            if(args.DropResult == Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move)
            {
                if (DataContext is ChecklistPageViewModel vm)
                {
                    await vm.AutoExport(vm.Checklist.UseEncryption);
                }
            }
        }

        private async void AssignedTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                if (sender is TextBox textBox && textBox.DataContext is ChecklistItem item && DataContext is ChecklistPageViewModel vm && textBox.Parent is Grid grid)
                {
                    item.Assigned = textBox.Text;
                    grid.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                    await vm.ItemAssignedChanged(item);
                }
            }
            else
            {
                if (sender is TextBox textBox && textBox.Parent is Grid grid)
                {
                    grid.Background = new SolidColorBrush(Color.FromArgb(50,255,0,0));
                }
            }
        }

        private void FavoriteChecklistClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.DataContext is Checklist checklist)
            {
                if (checklist.IsFavorited)
                {
                    ChecklistFavoriteCache.Current?.RemoveItem(checklist);
                    checklist.IsFavorited = false;
                }
                else
                {
                    ChecklistFavoriteCache.Current?.AddItem(checklist);
                    checklist.IsFavorited = true;
                }
            }
        }
    }
}
