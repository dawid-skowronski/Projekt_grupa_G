namespace SimpleApiBackend.Models
{
    public class PaymentRequestModel
    {
        public int DebtId { get; set; } // ID długu, który dotyczy żądania
        public int RequestedById { get; set; } // ID użytkownika, który składa żądanie
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow; // Data i czas złożenia żądania
        public string PaymentMethod { get; set; } // Gotówka, Blik, Revolut
        public string Status { get; set; } = "Pending"; // Status: Pending, Accepted, Rejected
    }
}
