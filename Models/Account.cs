namespace FinanceTracker.Models
{
    public class Account
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }       // Savings, Checking, Cash, Credit
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "USD";
        public string Color { get; set; }
        public bool IsActive { get; set; } = true;

        public override string ToString() => $"{Name} ({Balance:C})";
    }
}
