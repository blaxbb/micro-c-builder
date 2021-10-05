using MicroCBuilder.Models;
using MicroCBuilder.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace MicroCBuilder.ViewModels
{
    public class ChecklistPageViewModel : BaseViewModel
    {
        public ObservableCollection<ChecklistItem> Items { get; set; }
        public Command<ChecklistItem> EditItemCommand { get; }

        public ChecklistPageViewModel()
        {
            Items = new ObservableCollection<ChecklistItem>();

            Items.CollectionChanged += Items_CollectionChanged;

            Items.Add(new ChecklistItem()
            {   
                Name = "Task 1",
            });

            Items.Add(new ChecklistItem()
            {
                Name = "Task 2",
            });

            Items.Add(new ChecklistItem()
            {
                Name = "Task 3",
            });

            EditItemCommand = new Command<ChecklistItem>(async (item) =>
            {
                item = await ShowEditItemDialog(item);
            });
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine("Changed");
        }

        async Task<ChecklistItem?> ShowEditItemDialog()
        {
            return await ShowEditItemDialog(null);
        }

        async Task<ChecklistItem?> ShowEditItemDialog(ChecklistItem? existing)
        {
            var dialog = new ContentDialog();

            dialog.Title = existing == null ? "New Item" : $"Edit {existing.Name}";
            dialog.PrimaryButtonText = "Save";
            dialog.SecondaryButtonText = "Cancel";

            var control = new ChecklistItemEditControl();
            control.NameTextBox.Text = existing?.Name ?? "";

            dialog.Content = control;

            var result = await dialog.ShowAsync();

            if(result == ContentDialogResult.Primary)
            {
                if(existing == null)
                {
                    existing = new ChecklistItem();
                }
                existing.Name = control.NameTextBox.Text;
                return existing;
            }

            return null;
        }

        public async Task AddItem()
        {
            var newItem = await ShowEditItemDialog();
            if (newItem != null)
            {
                Items.Add(newItem);
            }
        }
    }
}
