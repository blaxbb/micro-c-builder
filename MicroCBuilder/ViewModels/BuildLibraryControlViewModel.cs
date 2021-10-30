using MicroCBuilder.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.ViewModels
{
    public class BuildLibraryControlViewModel : BaseViewModel
    {
        private string selectedAuthor;
        private SortFieldOptions sortField;
        private bool ascending;

        public List<ProductList> AllProductLists { get; set; }
        public ObservableCollection<ProductList> ProductLists { get; set; }
        public Command<ProductList> RemoveItem { get; }

        public ObservableCollection<string> Authors { get; }
        public string SelectedAuthor { get => selectedAuthor; set { SetProperty(ref selectedAuthor, value); FilterList(); } }

        public enum SortFieldOptions
        {
            Name,
            Author,
            Created,
            Price
        }

        public List<SortFieldOptions> SortFields { get; } = Enum.GetValues(typeof(SortFieldOptions)).Cast<SortFieldOptions>().ToList();
        public SortFieldOptions SortField { get => sortField; set { SetProperty(ref sortField, value); FilterList(); } }
        public bool Ascending { get => ascending; set => SetProperty(ref ascending, value); }
        public Command ToggleDirection { get; }

        public BuildLibraryControlViewModel()
        {
            ProductLists = new ObservableCollection<ProductList>();
            AllProductLists = new List<ProductList>();

            Authors = new ObservableCollection<string>();
            Authors.Add("None");

            SortField = SortFieldOptions.Price;
            Ascending = true;

            ToggleDirection = new Command((_) =>
            {
                Ascending = !Ascending;
                FilterList();
            });

            ProductLists.CollectionChanged += ProductLists_CollectionChanged;

            var dispatcher = Windows.UI.Xaml.Window.Current.Dispatcher;
            Task.Run(async () =>
            {
                var library = await BuildLibrary.GetLibrary();
                foreach (var entry in library)
                {
                    await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        AllProductLists.Add(entry);
                        FilterList();
                    });
                }
            });

            RemoveItem = new Command<ProductList>(async (list) =>
            {
                AllProductLists.Remove(list);
                FilterList();
                await BuildLibrary.RemoveLibraryEntry(list);
            });
        }

        private void FilterList()
        {
            ProductLists.Clear();
            var filtered = AllProductLists
                .Where(l => string.IsNullOrEmpty(SelectedAuthor) || SelectedAuthor == "None" || l.Author == SelectedAuthor);
            IOrderedEnumerable<ProductList> ordered;
            if (Ascending)
            {
                ordered = filtered.OrderBy(l => SortFieldValue(l));
            }
            else
            {
                ordered = filtered.OrderByDescending(l => SortFieldValue(l));
            }

            ordered.ToList().ForEach(l => ProductLists.Add(l)
            );
        }

        private object SortFieldValue(ProductList list)
        {
            switch (SortField)
            {
                default:
                case SortFieldOptions.Name:
                    return list.Name;
                case SortFieldOptions.Author:
                    return list.Author;
                case SortFieldOptions.Created:
                    return list.Created.Ticks;
                case SortFieldOptions.Price:
                    return list.Price;
            }
        }

        private void ProductLists_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (var list in ProductLists)
            {
                if (!Authors.Contains(list.Author))
                {
                    Authors.Add(list.Author);
                }
            }
        }
    }
}
