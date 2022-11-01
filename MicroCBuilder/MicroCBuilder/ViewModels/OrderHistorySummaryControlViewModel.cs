using MicroCBuilder.Models.OrderHistory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MicroCBuilder.ViewModels
{
    public class OrderHistorySummaryControlViewModel : BaseViewModel
    {
        private ObservableCollection<OrderHistorySummaryItem> items;

        public ObservableCollection<OrderHistorySummaryItem> Items { get => items; set => SetProperty(ref items, value); }

        public OrderHistorySummaryControlViewModel()
        {
            Items = new ObservableCollection<OrderHistorySummaryItem>();
            var dispatcher = MainWindow.Current.DispatcherQueue;
            OrderHistoryCache.SalesAssociateUpdatedEvent += (items, salesId) => dispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () => OrderHistoryUpdated(items, salesId));
            Task.Run(async () => await Load());
        }

        private void OrderHistoryUpdated(List<TransactionLineItem> items, string salesId)
        {
            var existing = Items.FirstOrDefault(i => i.Name == salesId);
            var index = Items.IndexOf(existing);
            if (index >= 0)
            {
                Items.RemoveAt(index);
                Items.Insert(index, new OrderHistorySummaryItem()
                {
                    Name = salesId,
                    Revenue = items.Sum(i => i.Total)
                });
            }
            else
            {
                Items.Add(new OrderHistorySummaryItem()
                {
                    Name = salesId,
                    Revenue = items.Sum(i => i.Total)
                });
            }
        }

        private async Task Load()
        {
            var data = await OrderHistoryCache.LoadSalesAssociate("bbarrett");
            var t = await OrderHistoryCache.LoadTransaction("1");
        }
    }

    public class OrderHistorySummaryItem : BaseViewModel
    {
        private string name;
        private float revenue;

        public string Name { get => name; set => SetProperty(ref name, value); }
        public float Revenue { get => revenue; set => SetProperty(ref revenue, value); }
    }
}