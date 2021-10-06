using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using MicroCLib.Models;
using Microsoft.Toolkit.Uwp.UI;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static MicroCLib.Models.BuildComponent;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MicroCBuilder.Views
{
    public sealed partial class SearchResults : UserControl, INotifyPropertyChanged
    {
        private List<Item> Results { get; set; }

        public int Count => Results.Count;
        public string Query
        {
            get { return (string)GetValue(QueryProperty); }
            set { SetValue(QueryProperty, value); HandleQuery(value); }
        }

        // Using a DependencyProperty as the backing store for Query.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty QueryProperty =
            DependencyProperty.Register("Query", typeof(string), typeof(SearchResults), new PropertyMetadata("", new PropertyChangedCallback(QueryChanged)));



        public List<Item> Items => BuildComponentCache.Current.FromType(ComponentType);
        public BuildComponent.ComponentType ComponentType
        {
            get { return (BuildComponent.ComponentType)GetValue(ComponentTypeProperty); }
            set { SetValue(ComponentTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ComponentType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ComponentTypeProperty =
            DependencyProperty.Register("ComponentType", typeof(BuildComponent.ComponentType), typeof(SearchResults), new PropertyMetadata(BuildComponent.ComponentType.CaseFan, new PropertyChangedCallback(ComponentChanged)));



        public IList<BuildComponent> BuildComponents
        {
            get { return (IList<BuildComponent>)GetValue(BuildComponentsProperty); }
            set { SetValue(BuildComponentsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BuildComponents.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BuildComponentsProperty =
            DependencyProperty.Register("BuildComponents", typeof(IList<BuildComponent>), typeof(SearchResults), new PropertyMetadata(null));



        private static void ComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is SearchResults comp)
            {
                comp.ComponentUpdated();
            }
        }

        public ICommand ItemSelected
        {
            get { return (ICommand)GetValue(ItemSelectedProperty); }
            set { SetValue(ItemSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemSelectedProperty =
            DependencyProperty.Register("ItemSelected", typeof(ICommand), typeof(SearchResults), new PropertyMetadata(null));

        public delegate void ItemSelectedEventArgs(object sender, Item item);
        public event ItemSelectedEventArgs OnItemSelected;

        public ObservableCollection<SearchFilter> Filters { get; } = new ObservableCollection<SearchFilter>();

        public SearchResults()
        {
            this.InitializeComponent();
            Results = new List<Item>();
            DataContext = this;
            dataGrid.CanUserSortColumns = true;
            Filters.CollectionChanged += (sender, args) => UpdateFilter();
            LocalSearch.Init();
        }

        protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Escape)
            {
                dataGrid.SelectedItem = null;
            }
        }

        private void ComponentUpdated()
        {
            Results.Clear();
            dataGrid.ItemsSource = null;

            LocalSearch.ReplaceItems(Items);
            Dictionary<string, List<string>> specs = new Dictionary<string, List<string>>();
            specs.Add("Stock", new List<string>() { "In Stock" });
            specs.Add("Brand", new List<string>());

            string[] ignoredspecs = { "SKU", "UPC", "Mfr Part#" };

            foreach (var i in Items)
            {
                i.Specs["Stock"] = i.Stock != "0" ? "In Stock\nAll Items" : "All Items";

                i.Specs["Brand"] = i.Brand;
                if (!specs["Brand"].Contains(i.Brand))
                {
                    specs["Brand"].Add(i.Brand);
                }

                foreach(var s in i.Specs)
                {
                    if (ignoredspecs.Contains(s.Key))
                    {
                        continue;
                    }

                    if (!specs.ContainsKey(s.Key))
                    {
                        specs.Add(s.Key, new List<string>());
                    }
                    var splitSpec = s.Value.Split(',', '\n');
                    foreach (var specValue in splitSpec)
                    {
                        if (!specs[s.Key].Contains(specValue))
                        {
                            specs[s.Key].Add(specValue);
                        }
                    }
                }
            }

            Filters.Clear();
            foreach(var i in FilterMenuBar.Items.ToList())
            {
                FilterMenuBar.Items.Remove(i);
            }

            var checkedChangedCallback = new DependencyPropertyChangedCallback((obj, prop) =>
            {
                if(obj is RadioMenuFlyoutItem radio)
                {
                    if (radio.IsChecked)
                    {
                        Filters.Add(radio.Tag as SearchFilter);
                    }
                    else
                    {
                        Filters.Remove(radio.Tag as SearchFilter);
                    }
                }
            });

            foreach (var kvp in specs)
            {
                var root = new MenuFlyoutSubItem() { Text = kvp.Key };
                if(SearchFilter.DefaultEnabled(ComponentType, kvp.Key))
                {
                    var filter = new SearchFilter(kvp.Key, default) { Options = kvp.Value };
                    filter.PropertyChanged += (sender, args) => UpdateFilter();
                    Filters.Add(filter);
                }
                foreach(var s in kvp.Value)
                {
                    var filter = new SearchFilter(kvp.Key, s) { Options = kvp.Value };
                    filter.PropertyChanged += (sender, args) => UpdateFilter();

                    var menuItem = new MenuFlyoutItem() { Text = s, Tag =  filter};
                    menuItem.Click += MenuItem_Click;

                    root.Items.Add(menuItem);
                }
                FilterMenuBar.Items.Add(root);
            }

            BuildComponent c;
            
            foreach(var hint in BuildComponentDependency.Dependencies.Where(d => d is FieldContainsDependency f && !d.GetType().IsSubclassOf(typeof(FieldContainsDependency))).Cast<FieldContainsDependency>())
            {
                BuildComponent? otherComponent = null;
                string? searchFieldName = null;
                string? otherFieldName = null;

                if (hint.FirstType == this.ComponentType)
                {
                    otherComponent = BuildComponents.FirstOrDefault(c => c.Type == hint.SecondType && c.Item != null);
                    searchFieldName = hint.FirstFieldName;
                    otherFieldName = hint.SecondFieldName;
                }
                else if (hint.SecondType == this.ComponentType)
                {
                    otherComponent = BuildComponents.FirstOrDefault(c => c.Type == hint.FirstType && c.Item != null);
                    searchFieldName = hint.SecondFieldName;
                    otherFieldName = hint.FirstFieldName;
                }
                
                if(otherComponent == null || string.IsNullOrWhiteSpace(searchFieldName) || string.IsNullOrWhiteSpace(otherFieldName))
                {
                    continue;
                }

                var filter = Filters.FirstOrDefault(f => f.Category == searchFieldName);
                if (filter != null && otherComponent != null)
                {
                    Debug.WriteLine($"Filter = {filter.Category} COMP = {otherComponent.Item.Name}");
                    if (otherComponent.Item.Specs.ContainsKey(otherFieldName))
                    {
                        var vals = otherComponent.Item.Specs[otherFieldName].Split('\n');
                        foreach(var val in vals)
                        {
                            var existing = filter.Options.FirstOrDefault(f => f.Contains(val));
                            if (existing != null)
                            {
                                filter.Value.Add(existing);
                            }
                        }
                    }
                }
            }

            var stockFilter = Filters.FirstOrDefault(f => f.Category == "Stock");
            if(stockFilter != null)
            {
                stockFilter.Value.Add("In Stock");
            }

            HandleQuery("");
        }

        private static IEnumerable<T> FindChildren<T>(DependencyObject element)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(element);
            if(childCount == 0)
            {
                yield break;
            }
            for(int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                if(child is T cast)
                {
                    yield return cast;
                }
                else
                {
                    foreach(var c in FindChildren<T>(child))
                    {
                        yield return c;
                    }
                }
            }
        }


        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuFlyoutItem item && item.Tag is SearchFilter filter)
            {
                var toRemove = Filters.Where(f => f.Category == filter.Category);
                toRemove.ToList().ForEach(f => Filters.Remove(f));
                Filters.Add(filter);
            }
        }

        private static void QueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchResults s)
            {
                s.Update();
            }
        }

        public void Update()
        {
            HandleQuery(Query);
        }

        private void HandleQuery(string query)
        {
            Results.Clear();
            if (string.IsNullOrWhiteSpace(query))
            {
                Results = new List<Item>(Items);
            }
            else
            {
                if(query.Length == 6)
                {
                    var skuMatch = Items.FirstOrDefault(i => i.SKU == query);
                    if(skuMatch != null)
                    {
                        Results.Add(skuMatch);
                    }
                }

                Results.AddRange(LocalSearch.Search(query, Items));
                
            }
            UpdateFilter();
            OnPropertyChanged(nameof(Count));
        }

        private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void dataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var i = dataGrid.SelectedIndex;
            if(i == -1)
            {
                return;
            }
            var source = dataGrid.ItemsSource as ObservableCollection<Item>;
            if(i >= source.Count)
            {
                return;
            }

            var item = source[i].CloneAndResetQuantity();
            System.Diagnostics.Debug.WriteLine(item.Name);
            ItemSelected?.Execute(item);
            OnItemSelected?.Invoke(this, item);
        }

        private void dataGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                dataGrid_DoubleTapped(sender, new DoubleTappedRoutedEventArgs());
            }
        }

        private Regex NumberConverter = new Regex("^(\\d+\\.?\\d*)");

        private void dataGrid_Sorting(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridColumnEventArgs e)
        {
            if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Ascending)
            {
                e.Column.SortDirection = DataGridSortDirection.Descending;
            }
            else
            {
                e.Column.SortDirection = DataGridSortDirection.Ascending;
            }

            foreach(var column in dataGrid.Columns)
            {
                if(column != e.Column)
                {
                    column.SortDirection = null;
                }
            }

            var asc = e.Column.SortDirection == DataGridSortDirection.Ascending;
            Func<Item, object>? sort = null;
            switch (e.Column.Header.ToString())
            {
                case "SKU":
                    sort = (i) => i.SKU;
                    break;
                case "Stock":
                    
                    sort = (i) =>
                    {
                        var match = NumberConverter.Match(i.Stock);
                        var f = match.Success ? float.Parse(match.Groups[1].Value) : 0;
                        if(f == 25 && i.Stock.Contains('+'))
                        {
                            f++;
                        }
                        return f;
                    };
                    break;
                case "Price":
                    sort = (i) => i.Price;
                    break;
                case "Name":
                    sort = (i) => i.Name;
                    break;
                case "Brand":
                    sort = (i) => i.Brand;
                    break;
                default:
                    Debug.WriteLine($"column not sorted {e.Column.Tag}");
                    dataGrid.ItemsSource = new ObservableCollection<Item>(Results);
                    break;
            }

            dataGrid.ItemsSource = asc ? new ObservableCollection<Item>(Results.Where(FilterPredicate).OrderBy(sort)) : new ObservableCollection<Item>(Results.Where(FilterPredicate).OrderByDescending(sort));
        }

        private void UpdateFilter()
        {
            dataGrid.ItemsSource = new ObservableCollection<Item>(Results.Where(FilterPredicate));
        }

        private Func<Item, bool> FilterPredicate => item => Filters.All(filter => {
            if(filter == null || filter.Value.Count(f => f != null) == 0)
            {
                return true;
            }

            return item.Specs.ContainsKey(filter.Category) && item.Specs[filter.Category].Split('\n').Any(s => filter.Value.Contains(s));
        });

        private void FilterRemoveButtonClick(object sender, RoutedEventArgs e)
        {
            if(sender is Button b && b.DataContext is SearchFilter filter)
            {
                Filters.Remove(filter);
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if(sender is CheckBox checkBox)
            {
                var parent = checkBox.FindAscendant<ListView>();
                var val = checkBox.DataContext?.ToString() ?? "";
                if (parent.DataContext is SearchFilter filter && !filter.Value.Contains(val))
                {
                    filter.Value.Add(val);
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var parent = checkBox.FindAscendant<ListView>();
                if (parent.DataContext is SearchFilter filter)
                {
                    filter.Value.Remove(checkBox.DataContext?.ToString() ?? "");
                }
            }
        }

        private void ListView_Loaded(object sender, RoutedEventArgs e)
        {
            if(sender is ListView listView && listView.DataContext is SearchFilter filter)
            {
                var cbs = FindChildren<CheckBox>(listView);
                foreach(var cb in cbs)
                {
                    cb.IsChecked = filter.Value.Contains(cb.Content?.ToString() ?? "");
                }
            }
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            foreach(var filter in Filters)
            {
                filter.Value.Clear();
            }
            Update();
            HandleQuery(Query);
        }
    }

    public class SearchFilter : INotifyPropertyChanged
    {
        private string category;
        private ObservableCollection<string> value = new ObservableCollection<string>();
        private List<string> options = new List<string>();

        public SearchFilter(string category, string value)
        {
            Category = category;
            if (value != null)
            {
                this.value = new ObservableCollection<string>() { value };
            }

            this.Value.CollectionChanged += (o, e) => OnPropertyChanged(nameof(Value));
        }

        public string Category { get => category; set => SetProperty(ref category, value); }
        public ObservableCollection<string> Value { get => value; }
        public List<string> Options { get => options; set => SetProperty(ref options, value); }

        private static string[] DEFAULT_FILTERS = new string[]
        {
            "Stock",
            "Brand"
        };

        private static string[] CPU_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Graphics Specifications",
            "Compatibility"
        };

        private static string[] MOBO_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Socket Type",
            "North Bridge",
            "Form Factor",
            "WiFi Support",
            "Thunderbolt Ports"
        };

        private static string[] RAM_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Memory Type",
            "Memory Capacity",
            "Memory Speed (MHz)",
            "CAS Latency",
            "LED Color"
        };

        private static string[] CASE_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Case Type",
            "Max Motherboard Size",
            "Color"
        };

        private static string[] PSU_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Wattage",
            "Form Factor",
            "Modular",
            "Rating"
        };

        private static string[] GPU_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "GPU Manufacturer",
            "GPU Chipset"
        };

        private static string[] SSD_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Capacity",
            "Interface",

        };

        private static string[] HDD_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Capacity",
            "Form Factor"
        };

        private static string[] CASEFAN_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Fan Size",
            "LED Color Details"
        };

        private static string[] OPERATINGSYSTEM_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Media Type"
        };

        private static string[] WATERCOOLINGKIT_FILTERS = new string[]
        {
            "Stock",
            "Brand",
            "Radiator Size",
        };

        public static bool DefaultEnabled(ComponentType componentType, string specCategory)
        {
            string[] items = null;
            items = componentType switch
            {
                ComponentType.CPU => CPU_FILTERS,
                ComponentType.Motherboard => MOBO_FILTERS,
                ComponentType.RAM => RAM_FILTERS,
                ComponentType.Case => CASE_FILTERS,
                ComponentType.PowerSupply => PSU_FILTERS,
                ComponentType.GPU => GPU_FILTERS,
                ComponentType.SSD => SSD_FILTERS,
                ComponentType.HDD => HDD_FILTERS,
                ComponentType.CaseFan => CASEFAN_FILTERS,
                ComponentType.OperatingSystem => OPERATINGSYSTEM_FILTERS,
                ComponentType.WaterCoolingKit => WATERCOOLINGKIT_FILTERS,
                _ => DEFAULT_FILTERS
            };

            return items != null && items.Contains(specCategory);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
