namespace SimpleApiBackend.Models
{
    public class UserTrip
    {
        public int UserId { get; set; } // Id użytkownika
        public User User { get; set; } // Nawigacja do encji User

        public int TripId { get; set; } // Id wyjazdu
        public Trip Trip { get; set; } // Nawigacja do encji Trip

        public bool IsOffline { get; set; }

    }
}

