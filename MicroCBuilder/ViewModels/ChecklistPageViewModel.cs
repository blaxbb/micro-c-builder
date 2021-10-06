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
        public ObservableCollection<string> Changes { get; }

        Dictionary<string, DateTime> AssignedUpdatedTimes { get; }

        public ChecklistPageViewModel()
        {
            Items = new ObservableCollection<ChecklistItem>();
            Changes = new ObservableCollection<string>();
            AssignedUpdatedTimes = new Dictionary<string, DateTime>();

            Items.CollectionChanged += Items_CollectionChanged;

            AddItem(new ChecklistItem()
            {   
                Name = "Task 1",
            });

            AddItem(new ChecklistItem()
            {
                Name = "Task 2",
            });

            AddItem(new ChecklistItem()
            {
                Name = "Task 3",
            });

            EditItemCommand = new Command<ChecklistItem>(async (item) =>
            {
                item = await ShowEditItemDialog(item);
            });
        }

        void AddItem(ChecklistItem item)
        {
            item.PropertyChanged += Item_PropertyChanged;
            Items.Add(item);
        }

        public void ItemAssignedChanged(ChecklistItem item)
        {
            Changes.Add($"CHANGED {item.Name} ASSIGNED TO {item.Assigned}");
            Debug.WriteLine(Changes.Last());
        }

        private async void Item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ChecklistItem item)
            {
                switch (e.PropertyName)
                {
                    case nameof(ChecklistItem.Name):

                        break;
                    case nameof(ChecklistItem.Complete):
                        Changes.Add($"CHANGED {item.Name} COMPLETE TO {(item.Complete ? "YES" : "NO")}");
                        Debug.WriteLine(Changes.Last());

                        break;
                }
            }
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
                AddItem(newItem);
            }
        }
    }
}
