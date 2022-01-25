using MicroCBuilder.Models.OrderHistory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.ViewModels.OrderHistory
{
    public class OrderHistoryPageViewModel : BaseViewModel
    {
        private List<CommissionLineItem> selectedTransactions;
        private SalesAssociateSummary selectedAssociate;

        public ObservableCollection<SalesAssociateSummary> SalesAssociatesSummaries { get; }
        public SalesAssociateSummary SelectedAssociate
        {
            get => selectedAssociate;
            set
            {
                SetProperty(ref selectedAssociate, value);
                SelectedTransactions = OrderHistoryCache.SalesAssociates.ContainsKey(selectedAssociate.Name) ? OrderHistoryCache.SalesAssociates[selectedAssociate.Name] : new List<CommissionLineItem>();
            }
        }
        public List<CommissionLineItem> SelectedTransactions { get => selectedTransactions; set => SetProperty(ref selectedTransactions, value); }

        public OrderHistoryPageViewModel()
        {
            SalesAssociatesSummaries = new ObservableCollection<SalesAssociateSummary>();
            OrderHistoryCache.SalesAssociateUpdatedEvent += OrderHistoryCacheUpdated;
            foreach (var kvp in OrderHistoryCache.SalesAssociates)
            {
                SalesAssociatesSummaries.Add(new SalesAssociateSummary(kvp.Key, kvp.Value));
            }
        }

        private void OrderHistoryCacheUpdated(List<Models.OrderHistory.CommissionLineItem> items, string salesId)
        {

        }
    }

    public class SalesAssociateSummary : BaseViewModel
    {
        private string name;
        private int transactionCount;
        private float revenueTotal;
        private int returnCount;

        public string Name { get => name; set => SetProperty(ref name, value); }
        public int TransactionCount { get => transactionCount; set => SetProperty(ref transactionCount, value); }
        public int ReturnCount { get => returnCount; set => SetProperty(ref returnCount, value); }
        public float RevenueTotal { get => revenueTotal; set => SetProperty(ref revenueTotal, value); }

        public SalesAssociateSummary()
        {

        }

        public SalesAssociateSummary(string name, List<CommissionLineItem> items)
        {
            Name = name;
            var grouped = items.GroupBy(i => i.Transaction);
            var total = grouped.Count();
            ReturnCount = grouped.Sum(i => i.FirstOrDefault()?.SaleType == "Return" ? 1 : 0);
            TransactionCount = total - returnCount;
            RevenueTotal = items.Sum(i => i.Total);
        }
    }
}
