namespace SimpleApiBackend.Models
{
    public class Expense
    {
        public int ExpenseId { get; set; }
        public int TripId { get; set; }
        public int CreatorId { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public string Location { get; set; }

        // Navigation properties
        public Trip Trip { get; set; }
        public User Creator { get; set; }
        public ICollection<Debt> Debts { get; set; }
    }

}
