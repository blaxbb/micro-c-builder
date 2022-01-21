using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.Models.OrderHistory
{

    public class CommissionLineItem
    {
        public TransactionNumber TransactionNumber { get; set; }
        public string SaleType { get; set; }
        public int Line { get; set; }
        public string Sku { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public float Amount { get; set; }
        public int GroupId { get; set; }
        public float Total { get; set; }
        public string Transaction => TransactionNumber?.ToString();
        public string id { get; set; }
    }

    public class TransactionNumber
    {
        public int StoreId { get; set; }
        public string Department { get; set; }
        public int TransactionId { get; set; }

        public override string ToString() => $"{StoreId}-{Department}-{TransactionId}";
    }
}
