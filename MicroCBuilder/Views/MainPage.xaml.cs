﻿using MicroCBuilder.Models;
using MicroCBuilder.ViewModels;
using MicroCLib.Models;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using static MicroCLib.Models.BuildComponent;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MicroCBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private string progressTitleText;
        private string progressElapsedText;
        private double progressElapsedValue;
        private Visibility progressVisibility;

        public static MainPage Instance { get; private set; }
        public static Grid PrintContainer => Instance?.printContainer;
        public static PrintHelper PrintHelper { get; set; }
        public string ProgressTitleText { get => progressTitleText; set => SetProperty(ref progressTitleText, value); }
        public string ProgressElapsedText { get => progressElapsedText; set => SetProperty(ref progressElapsedText, value); }
        public double ProgressElapsedValue { get => progressElapsedValue; set => SetProperty(ref progressElapsedValue, value); }
        public Visibility ProgressVisibility { get => progressVisibility; set => SetProperty(ref progressVisibility, value); }

        private bool UpdateRunningLock = false;

        private bool isLandingPage;
        private bool isBuildPage;
        private bool isSettingsPage;
        private bool isChecklistPage;

        public bool IsLandingPage { get => isLandingPage; set => SetProperty(ref isLandingPage, value); }
        public bool IsBuildPage { get => isBuildPage; set => SetProperty(ref isBuildPage, value); }
        public bool IsSettingsPage { get => isSettingsPage; set => SetProperty(ref isSettingsPage, value); }
        public bool IsChecklistPage { get => isChecklistPage; set => SetProperty(ref isChecklistPage, value); }

        public MainPage()
        {
            Instance = this;
            this.InitializeComponent();
            this.DataContext = this;
            ProgressVisibility = Visibility.Collapsed;
            InitCache();

            if (Settings.Categories().Count == 0)
            {
                PushTab<SettingsPage>("Settings");
            }
            else
            {
                Tabs_AddTabButtonClick(null, null);
            }

            Tabs.SelectionChanged += (sender, args) =>
            {
                switch (CurrentTabContent)
                {
                    case LandingPage:
                        IsLandingPage = true;
                        IsBuildPage = false;
                        IsSettingsPage = false;
                        IsChecklistPage = false;
                        break;
                    case BuildPage:
                        IsLandingPage = false;
                        IsBuildPage = true;
                        IsSettingsPage = false;
                        IsChecklistPage = false;
                        break;
                    case SettingsPage:
                        IsLandingPage = false;
                        IsBuildPage = false;
                        IsSettingsPage = true;
                        IsChecklistPage = false;
                        break;
                    case ChecklistPage:
                        IsLandingPage = false;
                        IsBuildPage = false;
                        IsSettingsPage = false;
                        IsChecklistPage = true;
                        break;
                    case OrderHistoryPage:
                        IsLandingPage = false;
                        isBuildPage = false;
                        isSettingsPage = false;
                        IsChecklistPage = false;
                        break;
                    case null:
                        break;
                    default:
                        Console.WriteLine($"Menu items not defined for type {CurrentTabContent.GetType().Name}");
                        break;
                }
            };

            MicroCBuilder.ViewModels.SettingsPageViewModel.ForceUpdate += async () => await UpdateCache();

            Settings.SettingsUpdated += (sender, args) => SetupAddButtons();
            SetupAddButtons();

            Tabs.TabCloseRequested += Tabs_TabCloseRequested1;

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            Window.Current.SetTitleBar(TabDragArea);
        }

        private void Tabs_TabCloseRequested1(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if(Tabs.TabItems.Count == 0)
            {
                Tabs_AddTabButtonClick(null, null);
            }
        }

        private void SetupAddButtons()
        {
            var flyoutItems = ((MenuFlyout)AddButton.Flyout).Items;
            flyoutItems.Clear();

            var componentTypes = Settings.Categories();
            foreach (var type in componentTypes)
            {
                flyoutItems.Add(new MenuFlyoutItem() { Text = type.ToString(), Command = new Command<ComponentType>(type => AddComponent(type)), CommandParameter = type });
            }

            flyoutItems.Add(new MenuFlyoutSeparator());
            flyoutItems.Add(new MenuFlyoutItem() { Text = "Search", Command = new Command((_) => SearchClicked()) });
            flyoutItems.Add(new MenuFlyoutItem() { Text = "Custom", Command = new Command((_) => CustomItemClicked()) });
        }

        private void AddComponent(ComponentType type)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.Add.Execute(type);
            }
        }
        private void SearchClicked()
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.AddSearchItem.Execute(null);
            }
        }
        private void CustomItemClicked()
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.AddCustomItem.Execute(null);
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                TabRightMargin.MinWidth = sender.SystemOverlayRightInset;
                TabLeftMargin.MinWidth = sender.SystemOverlayLeftInset;
            }
            else
            {
                TabRightMargin.MinWidth = sender.SystemOverlayLeftInset;
                TabLeftMargin.MinWidth = sender.SystemOverlayRightInset;
            }

            TabRightMargin.Height = TabLeftMargin.Height = sender.Height;
        }

        public async Task DisplayProgress(Func<IProgress<int>, Task> action, string title, int itemCount)
        {
            ProgressTitleText = title;
            var dispatcher = Window.Current.Dispatcher;

            var lastPercentCheck = 0f;
            TimeSpanRollingAverage average = new TimeSpanRollingAverage();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var progress = new Progress<int>((i) =>
            {
                ProgressVisibility = Visibility.Visible;
                var deltaTime = sw.Elapsed;
                var percent = 100 * i / (float)itemCount;
                var deltaPercent = percent - lastPercentCheck;
                var percentRemaining = 100 - percent;
                lastPercentCheck = percent;

                TimeSpan estimation;
                if (deltaPercent > 0)
                {
                    estimation = deltaTime * (percentRemaining / deltaPercent);
                }
                else
                {
                    estimation = TimeSpan.Zero;
                }
                average.Push(estimation);


                ProgressElapsedValue = percent;
                ProgressElapsedText = $"{i} / {itemCount} {average.Average:hh\\:mm\\:ss} remaining";
                sw.Restart();
            });

            ProgressVisibility = Visibility.Visible;
            await action(progress)
                .ContinueWith(async (_) =>
                {
                    await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        ProgressVisibility = Visibility.Collapsed;
                    });
                });
        }

        public async Task UpdateCache()
        {
            if (UpdateRunningLock)
            {
                return;
            }
            UpdateRunningLock = true;

            await DisplayProgress(async (progress) =>
            {
                await BuildComponentCache.Current.PopulateCache(progress);
            }, "Updating cache", 100);

            var categoryStrings = Settings.Categories().Select(c => BuildComponent.CategoryFilterForType(c)).ToList();
            await DisplayProgress(async (progress) =>
            {
                await BuildComponentCache.Current.DeepPopulateCache(progress);
            }, "Updating cache", BuildComponentCache.Current?.Cache.Where(kvp => categoryStrings.Contains(kvp.Key)).Sum(kvp => kvp.Value.Count) ?? 100);

            UpdateRunningLock = false;
        }

        public async void InitCache()
        {
            var progress = new Progress<int>((int p) =>
            {
                ProgressElapsedValue = p;
            });


            var dispatcher = Window.Current.Dispatcher;

            var checklistCache = new ChecklistFavoriteCache();
            await checklistCache.LoadCache();

            var cache = new BuildComponentCache();
            if (cache.Cache.Count == 0)
            {
                await DisplayProgress(async (progress) =>
                {
                    var ts = DateTime.Now - Settings.LastUpdated();
                    var cached = await cache.LoadCache();
                    if (!cached || ts.TotalHours >= 20)
                    {
                        await cache.PopulateCache(progress);
                    }
                }, "Updating cache", 100);
            }
            else
            {
                ProgressVisibility = Visibility.Collapsed;
            }
        }

        private object? CurrentTabContent
        {
            get
            {
                if (Tabs.SelectedItem is TabViewItem tabView && tabView.Content is Frame frame)
                {
                    return frame.Content;
                }

                return null;
            }
        }

        private T PushTab<T>(string title)
        {
            var tab = new TabViewItem()
            {
                Header = title,
            };

            var frame = new Frame();
            frame.HorizontalAlignment = HorizontalAlignment.Stretch;
            frame.VerticalAlignment = VerticalAlignment.Stretch;
            tab.Content = frame;
            frame.Navigate(typeof(T));

            Tabs.TabItems.Add(tab);
            Tabs.SelectedItem = tab;

            return (T)frame.Content;
        }

        public void CreateBuild(ProductList list)
        {
            if(list.Components == null)
            {
                return;
            }

            if (Tabs.SelectedIndex >= 0 && Tabs.SelectedIndex < Tabs.TabItems.Count)
            {
                Tabs.TabItems.RemoveAt(Tabs.SelectedIndex);
            }
            var buildPage = PushTab<BuildPage>(list.Name ?? "Build");
            if (buildPage.DataContext is BuildPageViewModel vm)
            {
                vm.InsertComponents(list.Components);
                vm.LibraryGuid = list.Guid;
                
            }
        }

        public void CreateChecklist(Checklist checklist)
        {
            if (Tabs.SelectedIndex >= 0 && Tabs.SelectedIndex < Tabs.TabItems.Count)
            {
                Tabs.TabItems.RemoveAt(Tabs.SelectedIndex);
            }
            var page = PushTab<ChecklistPage>("Checklist");
            if(page.DataContext is ChecklistPageViewModel vm)
            {
                vm.Checklist = checklist;
            }
        }

        public void CreateOrderHistory(string salesId)
        {
            if (Tabs.SelectedIndex >= 0 && Tabs.SelectedIndex < Tabs.TabItems.Count)
            {
                Tabs.TabItems.RemoveAt(Tabs.SelectedIndex);
            }
            var page = PushTab<OrderHistoryPage>("Order History");
        }

        private void Tabs_AddTabButtonClick(Microsoft.UI.Xaml.Controls.TabView sender, object args)
        {
            var page = PushTab<LandingPage>("Micro-C-Builder");
            page.OnCreateBuild += (sender, info) => CreateBuild(info);
        }

        private void Tabs_TabCloseRequested(Microsoft.UI.Xaml.Controls.TabView sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);
        }

        private async void LoadClicked(object sender, RoutedEventArgs args)
        {
            FileOpenPicker openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            openPicker.FileTypeFilter.Add(".build");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                List<BuildComponent> components = null;
                try
                {
                    var text = await Windows.Storage.FileIO.ReadTextAsync(file);
                    components = System.Text.Json.JsonSerializer.Deserialize<List<BuildComponent>>(text);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }

                if (components == null)
                {
                    try
                    {
                        var text = await Windows.Storage.FileIO.ReadTextAsync(file);
                        var list = System.Text.Json.JsonSerializer.Deserialize<ProductList>(text);
                        components = list.Components;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }
                if (components != null)
                {

                    if (CurrentTabContent is LandingPage)
                    {
                        Tabs.TabItems.RemoveAt(Tabs.SelectedIndex);
                    }

                    var buildPage = PushTab<BuildPage>(Path.GetFileNameWithoutExtension(file.Name) ?? "Build");
                    if (buildPage.DataContext is BuildPageViewModel vm)
                    {
                        vm.InsertComponents(components);
                    }
                }

            }
        }

        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.Save.Execute(null);
            }
        }

        private void SaveSignsClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.SaveSigns.Execute(null);
            }
        }

        private void PrintClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                page.PrintClicked();
            }
        }

        private void ResetClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                page.Reset();
                vm.Reset.Execute(null);
            }
        }

        private void ExportClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.ExportToMCOL.Execute(null);
            }
        }

        private void MailClicked(object sender, RoutedEventArgs e)
        {

        }
        private void SettingsClick(object sender, RoutedEventArgs e)
        {
            PushTab<SettingsPage>("Settings");
        }

        private void ExportToWebClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.ExportToWeb.Execute(null);
            }
        }

        private void ImportFromWebClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.ImportFromWeb.Execute(null);
            }
        }

        private void PrintBarcodesClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                page.PrintBarcodesClicked();
            }
        }

        private void PrintPromoClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                page.PromoPrintClicked();
            }
        }

        private void UpdatePricingClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
            {
                vm.UpdatePricing.Execute(null);
            }
        }

        private async void AddChecklistClicked(object sender, RoutedEventArgs e)
        {
            if(CurrentTabContent is ChecklistPage page && page.DataContext is ChecklistPageViewModel vm)
            {
                await vm.AddItem();
            }
        }

        private async void ExportChecklistClicked(object sender, RoutedEventArgs e)
        {
            if (CurrentTabContent is ChecklistPage page && page.DataContext is ChecklistPageViewModel vm)
            {
                await vm.ExportCurrent();
            }
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        public static void PrintHelper_Initialize()
        {
            PrintHelper = new PrintHelper(PrintContainer);

            PrintHelper.OnPrintCanceled += PrintHelper_OnPrintCanceled;
            PrintHelper.OnPrintFailed += PrintHelper_OnPrintFailed;
            PrintHelper.OnPrintSucceeded += PrintHelper_OnPrintSucceeded;
        }

        public static void PrintHelper_OnPrintSucceeded()
        {
            ReleasePrintHelper();
        }

        public static async void PrintHelper_OnPrintFailed()
        {
            ReleasePrintHelper();
            var dialog = new MessageDialog("Printing failed.");
            await dialog.ShowAsync();
        }

        public static void PrintHelper_OnPrintCanceled()
        {
            ReleasePrintHelper();
        }

        public static void ReleasePrintHelper()
        {
            PrintHelper?.Dispose();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
        Stopwatch sw = new Stopwatch();

        private async void Search_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                sw.Start();
                if (CurrentTabContent is BuildPage page && page.DataContext is BuildPageViewModel vm)
                {
                    Item item = default;
                    var text = SearchTextBox.Text;
                    if (Uri.IsWellFormedUriString(text, UriKind.Absolute))
                    {
                        var uri = new Uri(text);
                        var uriMatch = Regex.Match(text, "\\.com.*?(product.*)");
                        item = await Item.FromUrl($"/{uriMatch.Groups[1].Value}", Settings.StoreID());
                    }

                    var match = Regex.Match(text, "^(\\d{6})(?:(?:.{4}$)|$)");
                    if (match.Success)
                    {
                        text = match.Groups[1].Value;
                        item = BuildComponentCache.Current.FindItemBySKU(text);
                    }
                    match = Regex.Match(text, "^(\\d{12})$");
                    if (match.Success)
                    {
                        text = match.Groups[1].Value;
                        item = BuildComponentCache.Current.FindItemByUPC(text);
                    }

                    if (item == default)
                    {
                        await vm.HandleSearch(text);
                    }
                    else
                    {
                        Debug.WriteLine($"SW => {sw.Elapsed.TotalMilliseconds}");
                        vm.AddDuplicate(new BuildComponent() { Item = item, Type = item.ComponentType });
                    }
                    SearchTextBox.Text = "";
                }
                sw.Reset();
            }
        }
    }
}
