using System;

namespace FinanceTracker.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int CategoryId { get; set; }
        public string AccountName { get; set; }
        public string CategoryName { get; set; }
        public string CategoryColor { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }       // "Income" or "Expense"
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public override string ToString() => $"{Date:dd MMM yyyy} | {Description} | {Amount:C}";
    }
}
