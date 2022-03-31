﻿using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using MicroCBuilder.ViewModels;
using MicroCBuilder.Views;
using MicroCLib.Models;
using MicroCLib.Models.Reference;
using Microsoft.Toolkit.Uwp.Helpers;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Devices.AllJoyn;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace MicroCBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BuildPage : Page
    {
        BuildPageViewModel? vm => DataContext as BuildPageViewModel;
        private PrintHelper _printHelper;

        public BuildPage()
        {
            this.InitializeComponent();
            var dispatcher = Window.Current.Dispatcher;

            Task.Run(async () => {
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    await MainPage.Instance.UpdateCache();
                });
            });
        }

        public void QueryUpdated (BuildComponentControl control, string query)
        {
            vm.SelectedComponent = control.Component;
            vm.Query = query;
        }

        public void QuerySubmitted(BuildComponentControl control, string query)
        {
            vm.SelectedComponent = control.Component;
            vm.Query = query;
            SearchView.Update();

            SearchView.dataGrid.Focus(FocusState.Keyboard);
            SearchView.dataGrid.SelectedIndex = 0;
        }

        private void SearchView_OnItemSelected(object sender, Item item)
        {
            int index = ComponentListView.SelectedIndex + 1;
            if(index == ComponentListView.Items.Count)
            {
                index = 0;
            }
            var container = ComponentListView.ContainerFromIndex(index) as Control;
            container?.Focus(FocusState.Keyboard);
            //ComponentListView.Focus(FocusState.Keyboard);
        }

        public async Task PrintClicked()
        {
            if(vm.Components.Count(c => c.Item != null) == 0)
            {
                return;
            }

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            var cb = new CheckBox() { Content = "Export to MCOL" };
            var cbFooter = new CheckBox() { Content = "Include Footer", IsChecked = true };
            var salesIdTb = new TextBox() { PlaceholderText = "Sales ID (optional)" };

            grid.Children.Add(cb);
            grid.Children.Add(cbFooter);
            grid.Children.Add(salesIdTb);

            Grid.SetRow(cb, 0);
            Grid.SetRow(cbFooter, 1);
            Grid.SetRow(salesIdTb, 2);

            var dialog = new ContentDialog()
            {
                Title = "Print options",
                Content = grid,
                PrimaryButtonText = "Print",
                SecondaryButtonText = "Cancel"
            };
            salesIdTb.KeyDown += (sender, args) => { if (args.Key == Windows.System.VirtualKey.Enter) dialog.Hide(); };
            var result = await dialog.ShowAsync();
            var name = salesIdTb.Text;
            var doExport = cb.IsChecked;
            if (result != ContentDialogResult.Secondary)
            {
                if (!string.IsNullOrWhiteSpace(salesIdTb.Text))
                {
                    if (vm.LibraryGuid != default)
                    {
                        await BuildLibrary.SaveExisting(vm.LibraryGuid, vm.Components.ToList(), author: salesIdTb.Text);
                    }
                    else
                    {
                        var list = await BuildLibrary.SaveNew(vm.Components.ToList(), $"Customer Quote", salesIdTb.Text);
                        vm.LibraryGuid = list.Guid;
                    }                    
                }
                await DoPrintQuote(vm.Components.ToList(), name, doExport ?? false, cbFooter.IsChecked ?? false);

            }
        }

        public static async Task DoPrintQuote(List<BuildComponent> Components, string salesID = "", bool exportToMCOL = false, bool includeFooter = false)
        {
            var itemsCount = Components.Count(c => c.Item != null);
            if (itemsCount == 0)
            {
                return;
            }

            MainPage.PrintHelper_Initialize();

            int ITEMS_PER_PAGE = includeFooter ? 12 : 13;
            
            for(int i = 0; i < itemsCount; i += ITEMS_PER_PAGE)
            {
                //create a new page
                Grid page = new Grid
                {
                    Padding = new Thickness(40, 10, 40, 10)
                };

                //
                //force grid to full width
                //
                page.Children.Add(new Canvas() { Width = 1000 });
                TextBlock header = new TextBlock
                {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    TextAlignment = TextAlignment.Center
                };
                if (string.IsNullOrWhiteSpace(salesID))
                {
                    header.Text = $"Order created on {DateTime.Now:yyyy-MM-dd}.";
                }
                else
                {
                    header.Text = $"Order created on {DateTime.Now:yyyy-MM-dd} at the {Settings.Store()} MicroCenter Location.\nContact {salesID}@microcenter.com with additional questions.";
                }

                var buildContext = new MCOLBuildContext();
                if (exportToMCOL)
                {
                    await buildContext.AddComponents(Components);
                }

                float SubTotal = Components.Where(c => c?.Item != null).Sum(c => c.Item.Price * c.Item.Quantity);

                if (includeFooter)
                {
                    var footer = new BuildSummaryControl
                    {
                        SubTotal = SubTotal,
                        MCOLUrl = buildContext.TinyBuildURL
                    };

                    page.Children.Add(footer);
                    Grid.SetRow(footer, 2);
                }
                page.Children.Add(header);

                Grid.SetRow(header, 0);

                page.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                page.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                page.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                var contents = new Grid();
                var comps = Components.Where(c => c.Item != null).Skip(i).Take(ITEMS_PER_PAGE).ToList();
                for (int j = 0; j < comps.Count; j++)
                {
                    var comp = comps[j];

                    //get element from usercontrol
                    var pv = new PrintView();
                    var item = pv.printGrid;
                    pv.Content = null;

                    BuildComponent plan1, plan2;
                    if(comp.Type == BuildComponent.ComponentType.BuildService)
                    {
                        plan1 = PrintView.GetBuildPlan(3, Components);
                        plan2 = PrintView.GetBuildPlan(2, Components);
                    }
                    else
                    {
                        plan1 = PrintView.GetPlan(3, comp);
                        plan2 = PrintView.GetPlan(2, comp);
                    }

                    if (plan1 != null && plan2 != null)
                    {
                        item.DataContext = new
                        {
                            Item = comp.Item,
                            PlanString = $"${plan1?.Item.Price}/${plan2.Item.Price}"
                        };
                    }
                    else
                    {
                        item.DataContext = new
                        {
                            Item = comp.Item
                        };
                    }

                    //stick it in a border
                    var border = new Border
                    {
                        BorderThickness = new Thickness(1, 0, 1, 1),
                        BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black),
                        Child = item
                    };

                    //add it to the grid
                    contents.Children.Add(border);
                    contents.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    Grid.SetRow(border, j);
                }

                var border2 = new Border()
                {
                    Child = contents,
                    BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black),
                    BorderThickness = new Thickness(0, 1, 0, 0)
                };

                page.Children.Add(border2);
                Grid.SetRow(border2, 1);

                //add full grid as new page
                MainPage.PrintHelper.AddFrameworkElementToPrint(page);
            }

            var printHelperOptions = new PrintHelperOptions(true)
            {
                Orientation = Windows.Graphics.Printing.PrintOrientation.Portrait
            };

            await MainPage.PrintHelper.ShowPrintUIAsync("Print Quote", printHelperOptions);
        }

        public async Task PrintBarcodesClicked()
        {
            var grid = new Grid();
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.Margin = new Thickness(10);

            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });


            List<TextBox> serialInputs = new List<TextBox>();
            int row = 0;
            foreach(var comp in vm.Components.Where(c => c.Item != null))
            {
                var label = new TextBlock() {
                    Text = comp.Item.Name,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5)
                };

                grid.Children.Add(label);
                Grid.SetColumn(label, 0);
                Grid.SetRow(label, row);

                for (int i = 0; i < comp.Item.Quantity; i++)
                {
                    /*
                     * Create a text input for each quantity of each build component
                     */

                    var input = new TextBox() {
                        PlaceholderText = "Serial",
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        Text = comp.Serials.Count > i ? comp.Serials[i] : ""
                    };

                    input.KeyDown += (sender, args) =>
                    {
                        if(args.Key == Windows.System.VirtualKey.Enter)
                        {
                            FocusManager.TryMoveFocus(FocusNavigationDirection.Down, new FindNextElementOptions()
                            {
                                SearchRoot = grid
                            });
                        }
                    };
                    input.Tag = (comp, i);

                    serialInputs.Add(input);
                    grid.Children.Add(input);
                    Grid.SetColumn(input, 1);

                    grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    Grid.SetRow(input, row);
                    row++;
                }

            }

            var scrollView = new ScrollViewer();
            scrollView.Content = grid;
            scrollView.Width = 500;

            var dialog = new ContentDialog()
            {
                Title = "Print Barcodes",
                Content = scrollView,
                PrimaryButtonText = "Print",
                SecondaryButtonText = "Cancel",
                FullSizeDesired = true,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                foreach (var input in serialInputs)
                {
                    /*
                     * Pass the updated serial back into the BuildComponent so it can be saved for later
                     */
                    if (input.Tag is (BuildComponent comp, int i))
                    {
                        if (comp.Serials.Count > i)
                        {
                            comp.Serials[i] = input.Text;
                        }
                        else
                        {
                            comp.Serials.Add(input.Text);
                        }
                    }
                }

                List<(BuildComponent comp, int index)> entries = new List<(BuildComponent comp, int index)>();


                BuildComponent last = null;
                foreach(var entry in serialInputs.Select(s => s.Tag).Cast<(BuildComponent comp, int index)>())
                {
                    /*
                     * Only add a single print entry if there are no serial numbers, otherwise do a single line item per serial number.
                     */
                    if(entry.comp == last && string.IsNullOrEmpty(entry.comp.Serials[entry.index]))
                    {
                        continue;
                    }

                    if(string.IsNullOrEmpty(entry.comp.Serials[entry.index]))
                    {
                        last = entry.comp;
                    }

                    entries.Add(entry);
                }

                await DoPrintBarcodes(vm.Components.ToList(), entries);
            }
        }

        public static async Task DoPrintBarcodes(List<BuildComponent> Components, List<(BuildComponent comp, int index)> serials)
        {
            var itemsCount = Components.Count(c => c.Item != null);
            if (itemsCount == 0)
            {
                return;
            }

            MainPage.PrintHelper_Initialize();

            const int BARCODE_ITEMS_PER_PAGE = 9;

            int serialIndex = 0;

            for (int i = 0; i < itemsCount; i += BARCODE_ITEMS_PER_PAGE)
            {
                //create a new page
                Grid page = new Grid
                {
                    Padding = new Thickness(40, 10, 40, 10)
                };

                //
                //force grid to full width
                //
                page.Children.Add(new Canvas() { Width = 10000 });
                TextBlock header = new TextBlock
                {
                    TextWrapping = TextWrapping.WrapWholeWords,
                    TextAlignment = TextAlignment.Center
                };
                header.Text = $"Order created on {DateTime.Now:yyyy-MM-dd}.";

                float SubTotal = Components.Where(c => c?.Item != null).Sum(c => c.Item.Price * c.Item.Quantity);

                var footer = new BuildSummaryControl
                {
                    SubTotal = SubTotal
                };

                page.Children.Add(header);
                page.Children.Add(footer);

                Grid.SetRow(header, 0);
                Grid.SetRow(footer, 2);

                page.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                page.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                page.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                var contents = new Grid();
                //var comps = vm.Components.Where(c => c.Item != null).Skip(i).Take(BARCODE_ITEMS_PER_PAGE).ToList();
                var pageItems = serials.Skip(i).Take(BARCODE_ITEMS_PER_PAGE).ToList();
                for (int j = 0; j < pageItems.Count; j++)
                {
                    var lineItem = pageItems[j];
                    var serial = lineItem.comp.Serials[lineItem.index];
                    serialIndex++;
                    //get element from usercontrol
                    var pv = new BarcodePrintView();
                    var item = pv.printGrid;
                    pv.Content = null;
                    item.DataContext = lineItem.comp;
                    pv.SetImages(lineItem.comp.Item.SKU, serial);

                    //stick it in a border
                    var border = new Border
                    {
                        BorderThickness = new Thickness(1, 0, 1, 1),
                        BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black),
                        Child = item
                    };

                    //add it to the grid
                    contents.Children.Add(border);
                    contents.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                    Grid.SetRow(border, j);
                }

                var border2 = new Border()
                {
                    Child = contents,
                    BorderBrush = new SolidColorBrush(Windows.UI.Colors.Black),
                    BorderThickness = new Thickness(0, 1, 0, 0)
                };

                page.Children.Add(border2);
                Grid.SetRow(border2, 1);

                //add full grid as new page
                MainPage.PrintHelper.AddFrameworkElementToPrint(page);
            }

            var printHelperOptions = new PrintHelperOptions(true)
            {
                Orientation = Windows.Graphics.Printing.PrintOrientation.Portrait
            };

            await MainPage.PrintHelper.ShowPrintUIAsync("Print Quote", printHelperOptions);
        }

        public async Task PromoPrintClicked()
        {
            var stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 5 };

            var cb = new CheckBox() { Content = "Split page", IsChecked = true };
            var optLabel = new TextBlock() { Text = "Optional" };
            var buildTechTextBox = new TextBox() { PlaceholderText = "Build Tech" };
            var author = new TextBox() { PlaceholderText = "Author" };
            var extraTextBox = new TextBox() { PlaceholderText = "Annotation", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap };

            stack.Children.Add(cb);
            stack.Children.Add(optLabel);
            stack.Children.Add(author);
            stack.Children.Add(buildTechTextBox);
            stack.Children.Add(extraTextBox);

            var dialog = new ContentDialog()
            {
                Title = "Print options",
                Content = stack,
                PrimaryButtonText = "Print",
                SecondaryButtonText = "Cancel"
            };
            var result = await dialog.ShowAsync();
            var doSplit = cb.IsChecked ?? false;
            if (result == ContentDialogResult.Primary)
            {
                List<string> extraStrings = new List<string>();

                if (!string.IsNullOrWhiteSpace(buildTechTextBox.Text))
                {
                    extraStrings.Add($"Built by {buildTechTextBox.Text}");
                }
                if (!string.IsNullOrWhiteSpace(extraTextBox.Text))
                {
                    extraStrings.Add(extraTextBox.Text);
                }
                
                await DoPrintPromo(vm.Components.ToList(), extraStrings, doSplit);

                if (!string.IsNullOrWhiteSpace(author.Text))
                {
                    if (vm.LibraryGuid != default)
                    {
                        await BuildLibrary.SaveExisting(vm.LibraryGuid, vm.Components.ToList(), author: author.Text);
                    }
                    else
                    {
                        var list = await BuildLibrary.SaveNew(vm.Components.ToList(), "Promo", author.Text);
                        vm.LibraryGuid = list.Guid;
                    }
                }
            }
        }

        public static async Task DoPrintPromo(List<BuildComponent> Components, List<string> extraStrings, bool IsSplit)
        {
            var itemsCount = Components.Count(c => c.Item != null);
            if (itemsCount == 0)
            {
                return;
            }

            MainPage.PrintHelper_Initialize();

            Grid page = new Grid
            {
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0)
            };


            page.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            page.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            page.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            //
            //Stretch to full page
            //
            var cvs = new Canvas() { Width = 1000 };
            page.Children.Add(cvs);
            Grid.SetColumnSpan(cvs, 2);

            var topGrid = new Grid()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            page.Children.Add(topGrid);
            if (IsSplit)
            {
                topGrid.RenderTransformOrigin = new Point(.5, .5);
                topGrid.RenderTransform = new ScaleTransform() { ScaleY = -1, ScaleX = -1 };
            }

            topGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
            topGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            topGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(topGrid, 0);
            Grid.SetRow(topGrid, 0);

            var topHeader = new TextBlock()
            {
                Text = "Micro Center Custom Built PC",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalTextAlignment = TextAlignment.Center,
                FontSize = 32,
                Padding = new Thickness(0, 5, 0, 5)
            };
            topGrid.Children.Add(topHeader);
            Grid.SetColumn(topHeader, 0);
            Grid.SetColumnSpan(topHeader, 2);
            Grid.SetRow(topHeader, 0);

            var promoGrid = new Grid();

            double fontSize = 32;
            var promoItems = new List<TextBlock>();

            var cpu = Components.FirstOrDefault(c => c.Item != null && c.Type == BuildComponent.ComponentType.CPU);
            var gpu = Components.FirstOrDefault(c => c.Item != null && c.Type == BuildComponent.ComponentType.GPU);
            var ram = Components.Where(c => c.Item != null && c.Type == BuildComponent.ComponentType.RAM);
            var ssds = Components.Where(c => c.Item != null && c.Type == BuildComponent.ComponentType.SSD);
            var _case = Components.FirstOrDefault(c => c.Item != null && c.Type == BuildComponent.ComponentType.Case);

            if (cpu?.Item != null && cpu.Item.Specs.ContainsKey("Processor"))
            {
                promoItems.Add(new TextBlock() { Text = $"{cpu.Item.Specs["Processor"]}" });
            }
            if (gpu?.Item != null && gpu.Item.Specs.ContainsKey("GPU Chipset"))
            {
                promoItems.Add(new TextBlock() { Text = $"{gpu.Item.Specs["GPU Chipset"]}"});
            }
            if (ram != null && ram.Count(r => r.Item != null) > 0)
            {
                var total = ram.Where(r => r.Item != null && r.Item.Specs != null)
                    .Where(r => r.Item.Specs.ContainsKey("Memory Capacity"))
                    .Select(r => (r.Item.Specs["Memory Capacity"], r))
                    .Select(data => (Regex.Match(data.Item1, "(\\d+)\\s*G"), data.r))
                    .Where(data => data.Item1.Success)
                    .Sum(data => int.Parse(data.Item1.Groups[1].Value) * data.r.Item.Quantity);

                var first = ram.FirstOrDefault(r => r.Item != null);
                string speed = "";
                if(first?.Item != null && first.Item.Specs.ContainsKey("Memory Speed (MHz)"))
                {
                    speed = first.Item.Specs["Memory Speed (MHz)"];
                }
                promoItems.Add(new TextBlock() { Text = $"{total}GB {speed}Mhz RAM" });
            }
            foreach (var ssd in ssds)
            {
                if (ssd?.Item != null && ssd.Item.Specs.ContainsKey("Capacity") && ssd.Item.Specs.ContainsKey("Interface"))
                {
                    promoItems.Add(new TextBlock() { Text = $"{ssd.Item.Specs["Capacity"]} {ssd.Item.Specs["Interface"]} SSD" });
                }
            }
            if(_case?.Item != null)
            {
                promoItems.Add(new TextBlock() { Text = $"{_case.Item.Name}", TextWrapping = TextWrapping.WrapWholeWords, MaxLines = 1 });
            }

            foreach(var text in extraStrings.SelectMany(s => s.Split('\r')))
            {
                promoItems.Add(new TextBlock() { Text = text, TextWrapping = TextWrapping.NoWrap, MaxLines = 1 });
            }

            promoGrid.ColumnDefinitions.Add(new ColumnDefinition());
            promoGrid.VerticalAlignment = VerticalAlignment.Center;
            foreach(var tb in promoItems)
            {
                tb.FontSize = fontSize;
                promoGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                promoGrid.Children.Add(tb);
                Grid.SetRow(tb, promoGrid.RowDefinitions.Count - 1);
            }

            topGrid.Children.Add(promoGrid);
            Grid.SetRow(promoGrid, 1);
            Grid.SetColumn(promoGrid, 0);




            var priceGrid = new Grid();
            priceGrid.HorizontalAlignment = HorizontalAlignment.Right;
            priceGrid.VerticalAlignment = VerticalAlignment.Center;
            priceGrid.Margin = new Thickness(0);
            priceGrid.Padding = new Thickness(0);

            var plan = GetBuildPlan(Components);

            float SubTotal = Components.Where(c => c?.Item != null).Sum(c => c.Item.Price * c.Item.Quantity);

            var priceStrings = new List<string>()
            {
                $"${SubTotal:.00}",
            };
            if (plan != default)
            {
                priceStrings.Add($"2 Year System Coverage - ${plan.Tiers[0].Price}");
                priceStrings.Add($"3 Year System Coverage - ${plan.Tiers[1].Price}");
            }
            for(int i = 0; i < priceStrings.Count; i++)
            {
                var tb = new TextBlock() {
                    Text = priceStrings[i],
                    TextAlignment = TextAlignment.Right
                };
                if (i == 0)
                {
                    tb.FontSize = 64;
                }
                else
                {
                    tb.FontSize = 18;
                }

                priceGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
                priceGrid.Children.Add(tb);
                Grid.SetRow(tb, priceGrid.RowDefinitions.Count - 1);
            }

            topGrid.Children.Add(priceGrid);
            Grid.SetRow(priceGrid, 1);
            Grid.SetColumn(priceGrid, 1);

            var bottomGrid = new Grid()
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            page.Children.Add(bottomGrid);
            bottomGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            bottomGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(bottomGrid, 1);

            if (IsSplit)
            {
                var botHeader = new TextBlock()
                {
                    Text = "Micro Center Custom Built PC",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalTextAlignment = TextAlignment.Center,
                    FontSize = 32,
                    Padding = new Thickness(0, 5, 0, 5)
                };
                bottomGrid.Children.Add(botHeader);
                Grid.SetColumn(botHeader, 0);
                Grid.SetColumnSpan(botHeader, 2);
                Grid.SetRow(botHeader, 0);
            }
            else
            {
                var botPadding = new Frame()
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Padding = new Thickness(0, 5, 0, 5)
                };
                bottomGrid.Children.Add(botPadding);
                Grid.SetColumn(botPadding, 0);
                Grid.SetColumnSpan(botPadding, 2);
                Grid.SetRow(botPadding, 0);
            }

            var itemsGrid = new Grid();
            itemsGrid.VerticalAlignment = VerticalAlignment.Center;
            itemsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            itemsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(5, GridUnitType.Star) });
            itemsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            itemsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            itemsGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            foreach (var comp in Components.Where(c => c.Item != null))
            {
                var item = comp.Item;

                itemsGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                var strings = new string[]
                {
                    item.Brand,
                    item.Name,
                    $"${item.Price:.00}",
                    item.SKU,
                    $"Qty {comp.Item.Quantity}"
                };
                for(int i = 0; i < strings.Length; i++)
                {
                    var tb = new TextBlock() { Text = strings[i]};
                    itemsGrid.Children.Add(tb);
                    Grid.SetRow(tb, itemsGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(tb, i);

                    if(i >= 2)
                    {
                        tb.HorizontalTextAlignment = TextAlignment.Right;
                    }
                }
            }

            var tax = Settings.TaxRate() / 100;

            var footerItems = new (string name, string value)[]
            {
                (" "," "),
                ("Subtotal", $"${SubTotal:.00}"),
                ("Tax", $"${(tax * SubTotal):.00}"),
                ("Total", $"${((1 + tax) * SubTotal):.00}"),
            };

            foreach(var item in footerItems)
            {
                var tb1 = new TextBlock() { Text = item.name, HorizontalTextAlignment = TextAlignment.Right };
                var tb2 = new TextBlock() { Text = item.value, HorizontalTextAlignment = TextAlignment.Right };

                itemsGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                itemsGrid.Children.Add(tb1);
                itemsGrid.Children.Add(tb2);

                Grid.SetColumn(tb1, 1);
                Grid.SetColumn(tb2, 2);
                Grid.SetRow(tb1, itemsGrid.RowDefinitions.Count - 1);
                Grid.SetRow(tb2, itemsGrid.RowDefinitions.Count - 1);

            }

            bottomGrid.Children.Add(itemsGrid);
            Grid.SetRow(itemsGrid, 1);
            Grid.SetColumn(itemsGrid, 0);


            if (IsSplit)
            {
                var border = new Border() { BorderThickness = new Thickness(0, 1, 0, 0), BorderBrush = new SolidColorBrush(Color.FromArgb(125, 0, 0, 0)) };
                border.VerticalAlignment = VerticalAlignment.Bottom;
                Grid.SetColumnSpan(border, 2);
                Grid.SetColumn(border, 0);
                page.Children.Add(border);
            }

            MainPage.PrintHelper.AddFrameworkElementToPrint(page);

            var printHelperOptions = new PrintHelperOptions(true)
            {
                Orientation = Windows.Graphics.Printing.PrintOrientation.Portrait
            };

            await MainPage.PrintHelper.ShowPrintUIAsync("Print Quote", printHelperOptions);
        }

        private void PrintHelper_OnPrintSucceeded()
        {
            ReleasePrintHelper();
        }

        private async void PrintHelper_OnPrintFailed()
        {
            ReleasePrintHelper();
            var dialog = new MessageDialog("Printing failed.");
            await dialog.ShowAsync();
        }

        private void PrintHelper_OnPrintCanceled()
        {
            ReleasePrintHelper();
        }

        private void ReleasePrintHelper()
        {
            _printHelper.Dispose();
        }

        private static PlanReference? GetBuildPlan(IEnumerable<BuildComponent> components)
        {
            var planTotal = components.Where(c => c.Item != null && c.Type != BuildComponent.ComponentType.Plan && c.Type != BuildComponent.ComponentType.BuildService && c.Type != BuildComponent.ComponentType.OperatingSystem && SettingsPageViewModel.PresetBYO().Contains(c.Type)).Sum(c => c.Item.Price * c.Item.Quantity);
            return PlanReference.Get(PlanReference.PlanType.Build_Plan, planTotal);
        }

        public void Reset()
        {
            for(int i = 0; i < vm.Components.Count; i++)
            {
                var li = ComponentListView.ContainerFromIndex(i) as ListViewItem;
                if(li.ContentTemplateRoot is BuildComponentControl control)
                {
                    control.SetTextBox("");
                }
            }
        }

        private async void FlareTapped(object sender, TappedRoutedEventArgs e)
        {
            if (vm.Flare != null)
            {
                var url = $"https://microc.bbarrett.me/quotes/{vm.Flare.ShortCode}";
                await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
            }
        }
    }
}