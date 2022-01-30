using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroCBuilder.Models.OrderHistory
{
    public class Transaction
    {
        public TransactionNumber TransactionNumber { get; set; }
        public List<TransactionLineItem> TransactionItems { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}
