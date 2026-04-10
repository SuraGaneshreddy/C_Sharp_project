namespace FinanceTracker.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }   // "Income" or "Expense"
        public string Color { get; set; }  // Hex color
        public string Icon { get; set; }   // Emoji or symbol
        public bool IsActive { get; set; } = true;

        public override string ToString() => Name;
    }
}
