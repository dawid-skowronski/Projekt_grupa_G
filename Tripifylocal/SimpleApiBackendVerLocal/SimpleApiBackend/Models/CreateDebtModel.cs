namespace SimpleApiBackend.Models
{
    public class CreateDebtModel
    {
        public int ExpenseId { get; set; }
        public int UserId { get; set; }    
        public decimal Amount { get; set; } 
        public string Currency { get; set; } 
        public string Status { get; set; }   
    }
}
