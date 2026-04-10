using System;

namespace FinanceTracker.Models
{
    public class Budget
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryColor { get; set; }
        public decimal LimitAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal RemainingAmount => LimitAmount - SpentAmount;
        public double PercentageUsed => LimitAmount > 0 ? (double)(SpentAmount / LimitAmount) * 100 : 0;
        public bool IsOverBudget => SpentAmount > LimitAmount;

        public string Period => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }
}
