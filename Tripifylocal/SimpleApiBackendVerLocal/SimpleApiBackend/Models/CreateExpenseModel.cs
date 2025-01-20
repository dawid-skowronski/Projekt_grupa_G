namespace SimpleApiBackend.Models
{
    public class CreateExpenseModel
    {
        public int TripId { get; set; }
        public int CreatorId { get; set; }
        public decimal Cost { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public string? Location { get; set; }

    }
}
