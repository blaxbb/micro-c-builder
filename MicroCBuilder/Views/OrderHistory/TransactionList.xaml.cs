using MicroCBuilder.Models.OrderHistory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MicroCBuilder.Views.OrderHistory
{
    public sealed partial class TransactionList : UserControl
    {
        public TransactionList()
        {
            this.InitializeComponent();
            InitDataGrid();
        }

        private void InitDataGrid()
        {
            var numb = new TransactionNumber() { Department = "PO", StoreId = 141, TransactionId = 1234567 };
            var numb2 = new TransactionNumber() { Department = "PO", StoreId = 141, TransactionId = 1234567 };
            var lines = new List<CommissionLineItem>()
            {
                new CommissionLineItem()
                {
                    TransactionNumber = numb,
                    Description = "Item Description",
                    Amount = 99.99f,
                    Total = 199.98f,
                    Quantity = 2,
                    Sku = "123456"
                },
                new CommissionLineItem()
                {
                    TransactionNumber = numb,
                    Description = "Item Description 2",
                    Amount = 43.99f,
                    Total = 43.99f,
                    Quantity = 1,
                    Sku = "654321"
                },
                new CommissionLineItem()
                {
                    TransactionNumber = numb2,
                    Description = "Item Description",
                    Amount = 99.99f,
                    Total = 99.99f,
                    Quantity = 1,
                    Sku = "123456"
                },
                new CommissionLineItem()
                {
                    TransactionNumber = numb2,
                    Description = "Item Description 2",
                    Amount = 43.99f,
                    Total = 131.97f,
                    Quantity = 3,
                    Sku = "654321"
                }
            };

            var groups = lines
                .GroupBy(l => l.TransactionNumber)
                .Select(g => new { GroupName = g.Key, Items = g });

            var Items = new ObservableCollection<GroupInfoCollection<CommissionLineItem>>();
            foreach (var group in groups)
            {
                var collection = new GroupInfoCollection<CommissionLineItem>();
                collection.Key = group.GroupName;
                foreach (var item in group.Items)
                {
                    collection.Add(item);
                }
                Items.Add(collection);
            }

            CollectionViewSource groupedItems = new CollectionViewSource();
            groupedItems.IsSourceGrouped = true;
            groupedItems.Source = Items;

            dataGrid.ItemsSource = groupedItems.View;
        }

        private void dataGrid_LoadingRowGroup(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridRowGroupHeaderEventArgs e)
        {
            ICollectionViewGroup group = e.RowGroupHeader.CollectionViewGroup;
            var item = group.GroupItems[0] as CommissionLineItem;
            var total = group.GroupItems.Cast<CommissionLineItem>().Sum(l => l.Total);
            e.RowGroupHeader.PropertyValue = $"{item.TransactionNumber} ${total}";
        }
    }

    public class GroupInfoCollection<T> : ObservableCollection<T>
    {
        public object Key { get; set; }

        public new IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)base.GetEnumerator();
        }
    }
}
