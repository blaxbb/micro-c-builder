using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.Models.OrderHistory
{

    public class TransactionLineItem
    {
        public TransactionNumber TransactionNumber { get; set; }
        public string SaleType { get; set; }
        public int Line { get; set; }
        [JsonProperty("LineNumber")]
        private int LineNumber { set { Line = value; } }

        public string Sku { get; set; }
        [JsonProperty("ShortSku")]
        private string ShortSku { set { Sku = value; } }


        public string Description { get; set; }
        public int Quantity { get; set; }

        public float Amount { get; set; }
        [JsonProperty("Price")]
        private float Price { set { Amount = value; } }

        public int GroupId { get; set; }
        
        public float Total { get; set; }
        [JsonProperty("TotalPrice")]
        private float TotalPrice { set { Total = value; } }

        public string Transaction => TransactionNumber?.ToString();
        public string id { get; set; }
    }

    public class TransactionNumber
    {
        public int StoreId { get; set; }
        public string Department { get; set; }
        public int TransactionId { get; set; }

        public override string ToString() => $"{StoreId}-{Department}-{TransactionId}";


        public override int GetHashCode()
        {
            int hashCode = -978902512;
            hashCode = hashCode * -1521134295 + StoreId.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Department);
            hashCode = hashCode * -1521134295 + TransactionId.GetHashCode();
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            return obj is TransactionNumber number &&
                   StoreId == number.StoreId &&
                   Department == number.Department &&
                   TransactionId == number.TransactionId;
        }
    }
}
