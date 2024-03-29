﻿using MicroCLib.Models;
using MicroCBuilder.Views;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DataFlareClient;

using static MicroCLib.Models.BuildComponent.ComponentType;
using Newtonsoft.Json;
using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI.Xaml.Media;
using Windows.Web.Http;
using static MicroCLib.Models.BuildComponent;
using MicroCBuilder.Models;

namespace MicroCBuilder.ViewModels
{
    public class BuildPageViewModel : BaseViewModel
    {
        private BuildComponent selectedItem;

        public ObservableCollection<BuildComponent> Components { get; }
        public Guid LibraryGuid { get; set; }

        private string query;
        private Flare flare;
        private MCOLBuildContext buildContext;

        public ICommand Save { get; }
        public ICommand SaveSigns { get; }
        public ICommand Reset { get; }
        public ICommand Add { get; }
        public ICommand Remove { get; }
        public ICommand ItemSelected { get; }
        public ICommand ExportToMCOL { get; }
        public ICommand ItemValuesUpdated { get; }
        public ICommand RemoveFlyoutCommand { get; }
        public ICommand AddEmptyFlyoutCommand { get; }
        public ICommand AddDuplicateFlyoutCommand { get; }
        public ICommand InfoFlyoutCommand { get; }
        public ICommand AddSearchItem { get; }
        public ICommand AddCustomItem { get; }
        public ICommand ExportToWeb { get; }
        public ICommand ImportFromWeb { get; }
        public ICommand UpdatePricing { get; }

        public BuildComponent SelectedComponent { get => selectedItem; set => SetProperty(ref selectedItem, value); }

        public string Query { get => query; set => SetProperty(ref query, value); }

        public float SubTotal => Components.Where(c => c?.Item != null).Sum(c => c.Item.Price * c.Item.Quantity);

        public Flare Flare { get => flare; set => SetProperty(ref flare, value); }

        public BuildPageViewModel()
        {
            Components = new ObservableCollection<BuildComponent>();

            Settings.Categories().ForEach(c =>
            {
                var comp = new BuildComponent() { Type = c };
                comp.PropertyChanged += (sender, args) =>
                {
                    OnPropertyChanged(nameof(Components));
                };
                Components.Add(comp);
            });

            Save = new Command(DoSave);
            SaveSigns = new Command(async (o) => DoSaveSigns(Components.ToList()));

            Reset = new Command(DoReset);

            Remove = new Command<BuildComponent>(async (comp) => await DoRemove(comp));
            Add = new Command<BuildComponent.ComponentType>(AddItem);
            ItemSelected = new Command<Item>(async (Item item) => { await DoItemSelected(item); });

            RemoveFlyoutCommand = new Command<BuildComponent>(async (comp) => await DoRemove(comp));
            InfoFlyoutCommand = null;
            AddEmptyFlyoutCommand = new Command<BuildComponent.ComponentType>(AddItem);
            AddDuplicateFlyoutCommand = new Command<BuildComponent>(AddDuplicate);
            InfoFlyoutCommand = new Command<BuildComponent>(async (comp) =>
            {
                if (comp.Item != null && !string.IsNullOrWhiteSpace(comp.Item.URL))
                {
                    var success = await Windows.System.Launcher.LaunchUriAsync(new Uri($"https://microcenter.com{comp.Item.URL}"));
                }
            });

            ExportToMCOL = new Command(async (_) =>
            {
                var buildContext = new MCOLBuildContext();
                await buildContext.AddComponents(Components.ToList());

                var url = buildContext.BuildURL;
                if (string.IsNullOrWhiteSpace(url))
                {
                    return;
                }

                await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
            });

            ItemValuesUpdated = new Command((_) =>
            {
                UpdateHintsAndErrors();
                OnPropertyChanged(nameof(SubTotal));
            });

            AddSearchItem = new Command(DoAddSearchItem);
            AddCustomItem = new Command(DoAddCustomItem);
            ExportToWeb = new Command(DoExportToWeb);
            ImportFromWeb = new Command(DoImportFromWeb);
            UpdatePricing = new Command(async (_) => await DoUpdatePricing(_));
        }

        private async Task DoUpdatePricing(object obj)
        {
            foreach (var comp in Components.Where(c => c.Item != null))
            {
                var item = BuildComponentCache.Current.FindItemBySKU(comp.Item.SKU);
                if (item == null)
                {
                    var items = await Search.LoadEnhanced(comp.Item.SKU, Settings.StoreID(), "");
                    if (items.Items.Count == 1)
                    {
                        item = items.Items[0];
                    }
                }
                if (item != null)
                {
                    var qty = comp.Item.Quantity;
                    comp.Item = item.CloneAndResetQuantity();
                    comp.Item.Quantity = qty;
                    OnPropertyChanged(nameof(SubTotal));
                }
            }
        }

        private void UpdateHintsAndErrors()
        {
            foreach (var comp in Components)
            {
                comp.ErrorText = "";
                comp.HintText = "";
            }

            foreach (var dep in BuildComponentDependency.Dependencies)
            {
                var items = Components.Where(comp => comp.Item != null).Select(comp => comp.Item).ToList();
                var errors = dep.HasErrors(items);
                foreach (var result in errors)
                {
                    var matchingComp = Components.FirstOrDefault(comp => comp.Item == result.Primary);
                    matchingComp.ErrorText += result.Text.Replace("\n", ", ") + "\n\n";
                }

                foreach (var comp in Components)
                {
                    if (comp.Item != null)
                    {
                        continue;
                    }

                    var hint = dep.HintText(items, comp.Type);
                    if (!string.IsNullOrWhiteSpace(hint))
                    {
                        comp.HintText += hint.Replace("\n", ", ") + "\n\n";
                    }
                }
            }
        }

        private async Task DoItemSelected(Item item)
        {
            if (SelectedComponent != null)
            {
                SelectedComponent.Item = item;
                UpdateHintsAndErrors();
            }
            OnPropertyChanged(nameof(SubTotal));
        }

        private async void DoExportToWeb(object obj)
        {
            var sharedPassword = Settings.SharedPassword();
            var stack = new StackPanel() { Orientation = Orientation.Vertical };

            var tb = new TextBox() { PlaceholderText = "Title" };
            var useEncryption = new CheckBox() { Content = "Use Encryption" };

            stack.Children.Add(tb);
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
            Flare flare;
            if (useEncryption.IsChecked ?? false && !string.IsNullOrWhiteSpace(sharedPassword))
            {
                flare = EncryptedFlare.Create(JsonConvert.SerializeObject(Components.Where(c => c.Item != null).ToList()), AesInfo.FromPassword(sharedPassword));
            }
            else
            {
                flare = new Flare(JsonConvert.SerializeObject(Components.Where(c => c.Item != null).ToList()));
                if (!string.IsNullOrWhiteSpace(sharedPassword))
                {
                    flare.Sign(AesInfo.FromPassword(sharedPassword));
                }
            }
            flare.Tag = $"micro-c-{Settings.StoreID()}";
            flare.Title = tb.Text;

            var success = await flare.Post($"https://dataflare.bbarrett.me/api/Flare");

            if (!success)
            {
                flare = new Flare("") { ShortCode = 0000 };
            }

            Flare = flare;
            await Task.Delay(5 * 1000);
            Flare = null;
        }

        private async void DoImportFromWeb(object obj)
        {
            var tb = new TextBox() { PlaceholderText = "Short Code" };
            var dialog = new ContentDialog()
            {
                Title = "Import From web",
                Content = tb,
                PrimaryButtonText = "Import",
                SecondaryButtonText = "Cancel"
            };
            tb.KeyDown += (sender, args) => { if (args.Key == Windows.System.VirtualKey.Enter) dialog.Hide(); };
            var result = await dialog.ShowAsync();
            var shortCode = tb.Text;
            if (result == ContentDialogResult.Secondary)
            {
                return;
            }

            var flare = await Flare.GetShortCode("https://dataflare.bbarrett.me/api/Flare", shortCode);

            if (flare != null && flare.ShortCode.ToString() == shortCode)
            {
                var sharedPassword = Settings.SharedPassword();
                List<BuildComponent> imported;
                if (!string.IsNullOrWhiteSpace(sharedPassword))
                {
                    (var imp, var encrypted) = flare.TryDecrypt<List<BuildComponent>>(AesInfo.FromPassword(sharedPassword));
                    imported = imp;
                }
                else
                {
                    var json = flare.Data;
                    imported = JsonConvert.DeserializeObject<List<BuildComponent>>(json);
                }

                if (imported != null && imported.Count > 0)
                {
                    DoReset(null);
                    foreach (var comp in imported)
                    {
                        if (comp.Item == null)
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(comp.CategoryFilter))
                        {
                            comp.Type = BuildComponentCache.Current.FindType(comp.Item.SKU);
                        }

                        var existing = Components.FirstOrDefault(c => c.Type == comp.Type);
                        if (existing == null)
                        {
                            Components.Add(comp);
                        }
                        else
                        {
                            if (existing.Item == null)
                            {
                                existing.Item = comp.Item;
                            }
                            else
                            {
                                Components.Insert(Components.IndexOf(existing) + 1, comp);
                            }
                        }
                    }
                }
            }

            UpdateHintsAndErrors();
            OnPropertyChanged(nameof(SubTotal));
        }

        private async void DoAddCustomItem(object obj)
        {
            var name = new TextBox() { PlaceholderText = "Name" };
            var price = new NumberBox() { PlaceholderText = "Price" };
            var sku = new TextBox() { PlaceholderText = "SKU" };
            var type = new ComboBox() { PlaceholderText = "Component Type", ItemsSource = Settings.Categories(), HorizontalAlignment = HorizontalAlignment.Stretch };

            var ramPromoInfo = new Grid() { Visibility = Visibility.Collapsed };
            ramPromoInfo.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            ramPromoInfo.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            var ramCapacity = new NumberBox() { PlaceholderText = "Capacity", ValidationMode = NumberBoxValidationMode.InvalidInputOverwritten, HorizontalAlignment = HorizontalAlignment.Stretch };
            var ramSpeed = new TextBox() { PlaceholderText = "Speed (MHz)", HorizontalAlignment = HorizontalAlignment.Stretch };
            ramPromoInfo.Children.Add(ramCapacity);
            ramPromoInfo.Children.Add(ramSpeed);
            Grid.SetColumn(ramCapacity, 0);
            Grid.SetColumn(ramSpeed, 1);

            var panel = new StackPanel() { Orientation = Orientation.Vertical };
            panel.Children.Add(name);
            panel.Children.Add(price);
            panel.Children.Add(sku);
            panel.Children.Add(type);
            panel.Children.Add(ramPromoInfo);

            type.SelectionChanged += (sender, args) =>
            {
                if (type.SelectedValue is ComponentType t && t == RAM)
                {
                    ramPromoInfo.Visibility = Visibility.Visible;
                }
                else
                {
                    ramPromoInfo.Visibility = Visibility.Collapsed;
                }
            };

            var dialog = new ContentDialog
            {
                Title = "Add - Search",
                Content = panel,
                PrimaryButtonText = "Submit",
                SecondaryButtonText = "Cancel"
            };
            name.KeyDown += (sender, args) => { if (args.Key == Windows.System.VirtualKey.Enter) dialog.Hide(); };
            price.KeyDown += (sender, args) => { if (args.Key == Windows.System.VirtualKey.Enter) dialog.Hide(); };

            var dialogResult = await dialog.ShowAsync();
            if (dialogResult != ContentDialogResult.Secondary)
            {
                var selectedType = (ComponentType)(type.SelectedItem ?? ComponentType.Miscellaneous);
                var comp = new BuildComponent()
                {
                    Type = selectedType,
                    Item = new Item()
                    {
                        Name = name.Text,
                        Price = (float)(double.IsNaN(price.Value) ? 0d : price.Value),
                        SKU = string.IsNullOrWhiteSpace(sku.Text) ? "000000" : sku.Text,
                        ComponentType = selectedType
                    }
                };

                switch (comp.Type)
                {
                    case CPU:
                        comp.Item.Specs.Add("Processor", name.Text);
                        break;
                    case GPU:
                        comp.Item.Specs.Add("GPU Chipset", name.Text);
                        break;
                    case RAM:
                        comp.Item.Specs.Add("Memory Capacity", $"{ramCapacity.Value}GB");
                        comp.Item.Specs.Add("Memory Speed (MHz)", ramSpeed.Text);
                        break;
                    case SSD:
                        comp.Item.Specs.Add("Capacity", name.Text);
                        break;
                }
                AddDuplicate(comp);
            }

            UpdateHintsAndErrors();
        }

        private async void DoAddSearchItem(object obj)
        {
            var dispatcher = Window.Current.Dispatcher;

            var tb = new TextBox() { PlaceholderText = "Search query" };
            var dialog = new ContentDialog
            {
                Title = "Add - Search",
                Content = tb,
                PrimaryButtonText = "Submit",
                SecondaryButtonText = "Cancel"
            };

            tb.KeyDown += (sender, args) => { if (args.Key == Windows.System.VirtualKey.Enter) dialog.Hide(); };

            var dialogResult = await dialog.ShowAsync();
            var query = tb.Text;

            if (dialogResult != ContentDialogResult.Secondary && !string.IsNullOrWhiteSpace(query))
            {
                await HandleSearch(query);
            }
        }

        public async Task HandleSearch(string query)
        {
            MicroCLib.Models.SearchResults? results = null;

            await MainPage.Instance.DisplayProgress(async (progress) =>
            {
                results = await Search.LoadEnhanced(query, Settings.StoreID(), null);
            }, $"Searching for {query}", 1);


            if (results != null)
            {
                Debug.WriteLine($"{results.Items.Count} RESULTS");
                Item? item = null;
                if (results.Items.Count == 1)
                {
                    item = results.Items.First();
                }
                else if (results.Items.Count > 0)
                {
                    item = await DisplaySearchResults(results.Items);
                }
                else
                {
                    var msg = new ContentDialog()
                    {
                        Title = "No results found.",
                        PrimaryButtonText = "Ok"
                    };
                    await msg.ShowAsync();
                }

                if (item != null)
                {
                    var comp = new BuildComponent() { Type = item.ComponentType, Item = item };
                    AddDuplicate(comp);

                }
            }

            UpdateHintsAndErrors();
        }

        private static async Task<Item?> DisplaySearchResults(List<Item> items)
        {
            var listView = new ListView()
            {
                ItemsSource = items
            };

            var dialog = new ContentDialog
            {
                Title = "Search Results",
                PrimaryButtonText = "Submit",
                SecondaryButtonText = "Cancel",
                Content = listView
            };

            listView.PreviewKeyDown += (sender, args) =>
            {
                if (args.Key == Windows.System.VirtualKey.Enter)
                {
                    dialog.Hide();
                    args.Handled = true;
                }
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Secondary)
            {
                return listView.SelectedItem as Item;
            }
            return null;
        }

        private async Task DoRemove(BuildComponent comp)
        {
            if (comp == null)
            {
                return;
            }

            comp.Item = null;
            if (comp.Type == BuildComponent.ComponentType.Miscellaneous || comp.Type == BuildComponent.ComponentType.Plan || Components.Count(c => c.Type == comp.Type) > 1)
            {
                Components.Remove(comp);
            }

            OnPropertyChanged(nameof(SubTotal));
            UpdateHintsAndErrors();
        }

        private void AddItem(BuildComponent.ComponentType type)
        {
            SelectedComponent = InsertAtEndByType(type);
            UpdateHintsAndErrors();
        }

        public void AddDuplicate(BuildComponent orig)
        {
            var refSku = orig.Item.Specs.ContainsKey("Ref") ? orig.Item.Specs["Ref"] : "";
            BuildComponent comp;
            if (refSku == "BUILD" && orig.Item.Specs.ContainsKey("Duration"))
            {
                orig = PrintView.GetBuildPlan(int.Parse(orig.Item.Specs["Duration"]), Components);
                comp = InsertAtIndex(orig.Type, 1);
            }
            else
            {
                comp = InsertAtSKUIndex(orig.Type, refSku);
            }
            comp.Item = orig.Item?.CloneAndResetQuantity();
            SelectedComponent = comp;
            OnPropertyChanged(nameof(SubTotal));
            UpdateHintsAndErrors();
        }

        private BuildComponent InsertAtEndByType(BuildComponent.ComponentType type)
        {
            int index = Components.Count;
            for (int i = Components.Count - 1; i >= 0; i--)
            {
                var existing = Components[i];
                if (existing.Type == type)
                {
                    index = i + 1;
                    break;
                }
            }

            var comp = new BuildComponent() { Type = type };
            comp.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(Components));
            };
            Components.Insert(index, comp);
            OnPropertyChanged(nameof(SubTotal));
            return comp;
        }

        private BuildComponent InsertAtSKUIndex(BuildComponent.ComponentType type, string sku)
        {
            var item = Components.FirstOrDefault(c => c.Item != null && c.Item.SKU == sku);
            if (item == null)
            {
                return InsertAtEndByType(type);
            }
            var index = Components.IndexOf(item);
            if (index < 0)
            {
                return InsertAtEndByType(type);
            }

            var comp = new BuildComponent() { Type = type };
            comp.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(Components));
            };
            Components.Insert(index + 1, comp);
            OnPropertyChanged(nameof(SubTotal));
            return comp;
        }

        private BuildComponent InsertAtIndex(BuildComponent.ComponentType type, int index)
        {
            var comp = new BuildComponent() { Type = type };
            comp.PropertyChanged += (sender, args) =>
            {
                OnPropertyChanged(nameof(Components));
            };
            Components.Insert(index, comp);
            OnPropertyChanged(nameof(SubTotal));
            return comp;
        }

        private void DoReset(object obj)
        {
            foreach (var c in Components)
            {
                c.Item = null;
            }
            OnPropertyChanged(nameof(SubTotal));
            UpdateHintsAndErrors();
        }

        public void InsertComponents(List<BuildComponent> fromFile)
        {
            foreach (var loadedComp in fromFile)
            {
                bool found = false;
                if (loadedComp.Type == BuildComponent.ComponentType.None && loadedComp.Item != null && loadedComp.Item.ComponentType != None)
                {
                    loadedComp.Type = loadedComp.Item.ComponentType;
                }

                foreach (var oldComp in Components)
                {
                    if (oldComp.Type == loadedComp.Type && oldComp.Item == null)
                    {
                        oldComp.Item = loadedComp.Item;
                        oldComp.Serials = loadedComp.Serials;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    var comp = InsertAtEndByType(loadedComp.Type);
                    comp.Item = loadedComp.Item;
                    comp.Serials = loadedComp.Serials;
                }
            }

            UpdateHintsAndErrors();
            OnPropertyChanged(nameof(SubTotal));
        }

        private async void DoSave(object obj)
        {
            var stack = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Spacing = 10
            };

            var authorTb = new TextBox() { PlaceholderText = "Author (Optional)" };
            var nameTb = new TextBox() { PlaceholderText = "File name" };

            stack.Children.Add(nameTb);
            stack.Children.Add(authorTb);

            var dialog = new ContentDialog()
            {
                Title = "Save Signs",
                Content = stack,
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Cancel"
            };

        showDialog:
            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(nameTb.Text))
            {
                goto showDialog;
            }


            var savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName = nameTb.Text
            };

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("MCBuild", new List<string>() { ".build" });
            // Default file name if the user does not type one in or select a file to replace

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until
                // we finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);

                // write to file
                var list = new ProductList(Components.Where(c => c.Item != null).ToList())
                {
                    Name = nameTb.Text,
                    Author = authorTb.Text
                };

                await BuildLibrary.Save(file, list);
                LibraryGuid = list.Guid;

                // Let Windows know that we're finished changing the file so
                // the other app can update the remote version of the file.
                // Completing updates may require Windows to ask for user input.
                Windows.Storage.Provider.FileUpdateStatus status =
                    await CachedFileManager.CompleteUpdatesAsync(file);
                if (status == Windows.Storage.Provider.FileUpdateStatus.Complete)
                {
                    Debug.WriteLine("File " + file.Name + " was saved.");
                }
                else
                {
                    Debug.WriteLine("File " + file.Name + " couldn't be saved.");
                }
            }
            else
            {
                Debug.WriteLine("Operation cancelled.");
            }
        }

        public static async void DoSaveSigns(List<BuildComponent> Components, string? title = null)
        {

            var signControl = new ExportSignsControl(title);

            var dialog = new ContentDialog()
            {
                Title = "Save Signs",
                Content = signControl,
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Cancel"
            };

        showDialog:
            var result = await dialog.ShowAsync();

            if (!signControl.SavePassword)
            {
                Settings.SignUsername("");
                Settings.SignPassword("");
            }

            if (result != ContentDialogResult.Primary)
            {
                return;
            }

            var checkStrings = new string[] { signControl.Title, signControl.Username, signControl.Password, signControl.BaseUrl, signControl.SignType };
            var checkStringsOutput = new string[] { nameof(signControl.Title), nameof(signControl.Username), nameof(signControl.Password), nameof(signControl.BaseUrl), nameof(signControl.SignType) };

            for (int i = 0; i < checkStrings.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(checkStrings[i]))
                {
                    Windows.UI.Popups.MessageDialog msg = new Windows.UI.Popups.MessageDialog($"{checkStringsOutput[i]} cannot be empty!", "Error");
                    await msg.ShowAsync();
                    goto showDialog;
                }
            }

            Settings.SignBaseUrl(signControl.BaseUrl);

            try
            {
                var url = await SignPublisher.Publish(
                skus: Components.Where(c => c.Item != null)
                    .SelectMany(c =>
                    {
                        var ret = new List<string>();
                        var qty = c.Item.Quantity;
                        if (!signControl.UseQuantity)
                        {
                            qty = 1;
                        }

                        for (int i = 0; i < qty; i++)
                        {
                            ret.Add(c.Item.SKU);
                        }
                        return ret;
                    })
                    .ToList(),
                    baseUrl: signControl.BaseUrl,
                    signType: signControl.SignType,
                    username: signControl.Username,
                    password: signControl.Password,
                    batchName: signControl.Title
                );

                if (string.IsNullOrWhiteSpace(url))
                {
                    Windows.UI.Popups.MessageDialog msg = new Windows.UI.Popups.MessageDialog("Failed to export signs!", "Error");
                    await msg.ShowAsync();
                }
                else
                {
                    if (signControl.SavePassword)
                    {
                        Settings.SignUsername(signControl.Username);
                        Settings.SignPassword(signControl.Password);
                    }
                    await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
                }
            }
            catch (Exception ex)
            {
                Windows.UI.Popups.MessageDialog msg = new Windows.UI.Popups.MessageDialog(ex.Message, "Error");
                await msg.ShowAsync();
            }

        }
    }
}
