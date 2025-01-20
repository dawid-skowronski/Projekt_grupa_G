namespace SimpleApiBackend.Models
{
    public class Debt
    {
        public int DebtId { get; set; }
        public int ExpenseId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }

       
        public Expense Expense { get; set; }
        public User User { get; set; }
    }

}
