namespace SimpleApiBackend.Models
{
    public class DebtPaymentRequest
    {
        public int Id { get; set; } // Klucz główny
        public int DebtId { get; set; } // Powiązanie z długiem
        public Debt Debt { get; set; } // Nawigacja do długu
        public int RequestedById { get; set; } // ID użytkownika zgłaszającego płatność
        public User RequestedBy { get; set; } // Nawigacja do użytkownika
        public string Status { get; set; } // Status (Pending, Accepted, Rejected)
        public string PaymentMethod { get; set; } // Gotówka, Blik, Revolut
        public DateTime RequestedAt { get; set; } // Data zgłoszenia
    }
}
