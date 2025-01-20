namespace SimpleApiBackend.Models
{
    public class Trip
    {
        public int TripId { get; set; } // Unikalny identyfikator wyjazdu
        public int CreatorId { get; set; } // ID użytkownika, który stworzył wyjazd
        public string SecretCode { get; set; } // Kod dołączania do wyjazdu
        public string Name { get; set; } // Nazwa wyjazdu
        public string Description { get; set; } // Opis wyjazdu
        public DateTime StartDate { get; set; } // Data rozpoczęcia wyjazdu
        public DateTime EndDate { get; set; } // Data zakończenia wyjazdu

        // Nawigacja do UserTrip (relacja wiele do wielu)
        public List<UserTrip> UserTrips { get; set; } = new List<UserTrip>();
        public ICollection<Invitation> Invitations { get; set; }
        public ICollection<Expense> Expenses { get; set; }
    }
}
