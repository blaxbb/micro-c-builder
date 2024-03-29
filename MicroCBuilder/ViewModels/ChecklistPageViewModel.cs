﻿using DataFlareClient;
using MicroCBuilder.Models;
using MicroCBuilder.Views;
using Newtonsoft.Json;
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
        private Checklist checklist;

        public Checklist Checklist { get => checklist; set { checklist?.Items.ToList().ForEach(c => c.PropertyChanged -= Item_PropertyChanged); SetProperty(ref checklist, value); checklist?.Items.ToList().ForEach(c => c.PropertyChanged += Item_PropertyChanged); } }
        public Command<ChecklistItem> EditItemCommand { get; }
        public Command<ChecklistItem> DeleteItemCommand { get; }

        Dictionary<string, DateTime> AssignedUpdatedTimes { get; }

        public bool UseEncryption { get; set; }

        public ChecklistPageViewModel()
        {
            AssignedUpdatedTimes = new Dictionary<string, DateTime>();
            Checklist = new Checklist();

            EditItemCommand = new Command<ChecklistItem>(async (item) =>
            {
                item = await ShowEditItemDialog(item);
            });

            DeleteItemCommand = new Command<ChecklistItem>(async (item) =>
            {
                Checklist?.Items.Remove(item);
                await AutoExport(Checklist.UseEncryption);
            });

            FlareHubManager.Subscribe($"micro-c-checklist-{Settings.StoreID()}");
            var dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher;
            FlareHubManager.OnFlareReceived += async (flare) =>
            {
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    AddChecklistFlare(flare);
                });
            };
        }

        void AddChecklistFlare(Flare flare)
        {
            if (flare.Tag != $"micro-c-checklist-{Settings.StoreID()}")
            {
                return;
            }

            var sharedPassword = Settings.SharedPassword();

            AesInfo? aesInfo = null;
            if (!string.IsNullOrWhiteSpace(sharedPassword))
            {
                aesInfo = AesInfo.FromPassword(sharedPassword);
            }

            Checklist newChecklist = null;

            if (aesInfo != null)
            {
                var (data, encrypted) = flare.TryDecrypt<Checklist>(aesInfo);
                newChecklist = data;
            }
            else
            {
                try
                {
                    newChecklist = (Checklist)flare.Value(typeof(Checklist));
                }
                catch (Exception ex)
                {
                    //flare is likely encrypted (or possibly malformed)
                }
            }

            if (newChecklist == null)
            {
                return;
            }
            newChecklist.Created = flare.Created;
            newChecklist.IsFavorited = ChecklistFavoriteCache.Current?.IsFavorited(newChecklist) ?? false;


            if((Checklist == null || Checklist.Id == newChecklist.Id) && newChecklist != Checklist)
            {
                Checklist = newChecklist;
            }
        }

        async Task AddItem(ChecklistItem item)
        {
            if (Checklist != null)
            {
                item.PropertyChanged += Item_PropertyChanged;
                Checklist.Items.Add(item);
                await AutoExport(Checklist.UseEncryption);
            }
        }

        public async Task ItemAssignedChanged(ChecklistItem item)
        {
            await AutoExport(checklist.UseEncryption);
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
                        await AutoExport(checklist.UseEncryption);
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

            if (result == ContentDialogResult.Primary)
            {
                if (existing == null)
                {
                    existing = new ChecklistItem();
                    existing.Name = control.NameTextBox.Text;
                    await AddItem(existing);
                }
                else
                {
                    existing.Name = control.NameTextBox.Text;
                    await AutoExport(Checklist.UseEncryption);
                }
                return existing;
            }

            return null;
        }

        public async Task AddItem()
        {
            var newItem = await ShowEditItemDialog();
        }

        public async Task ExportCurrent()
        {
            var stack = new StackPanel() { Orientation = Orientation.Vertical };

            var tb = new TextBox() { PlaceholderText = "Title", Text = Checklist?.Name ?? "" };
            var useEncryption = new CheckBox() { Content = "Use Encryption" };

            stack.Children.Add(tb);

            var sharedPassword = Settings.SharedPassword();
            if (!string.IsNullOrWhiteSpace(sharedPassword))
            {
                stack.Children.Add(useEncryption);
            }

            var dialog = new ContentDialog()
            {
                Title = "Export to web",
                Content = stack,
                PrimaryButtonText = "Export",
                SecondaryButtonText = "Cancel"
            };
            tb.KeyDown += (sender, args) => { if (args.Key == Windows.System.VirtualKey.Enter) dialog.Hide(); };
            var result = await dialog.ShowAsync();
            var name = tb.Text;
            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            await AutoExport(useEncryption.IsChecked ?? false);
        }

        public async Task AutoExport(bool encrypt)
        {
            var sharedPassword = Settings.SharedPassword();
            Flare flare;
            var payload = JsonConvert.SerializeObject(Checklist, new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
            });
            if (encrypt && !string.IsNullOrWhiteSpace(sharedPassword))
            {
                checklist.UseEncryption = true;
                flare = EncryptedFlare.Create(payload, AesInfo.FromPassword(sharedPassword));
            }
            else
            {
                flare = new Flare(payload);
                checklist.UseEncryption = false;
                if (!string.IsNullOrWhiteSpace(sharedPassword))
                {
                    flare.Sign(AesInfo.FromPassword(sharedPassword));
                }
            }
            flare.Tag = $"micro-c-checklist-{Settings.StoreID()}";
            flare.Title = Checklist.Name;

            var success = await flare.Post($"https://dataflare.bbarrett.me/api/Flare");
        }
    }
}
