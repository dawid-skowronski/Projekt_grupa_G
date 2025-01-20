namespace SimpleApiBackend.Models
{
    public class CreateTripResponse
    {
        public int TripId { get; set; }
        public string SecretCode { get; set; } = string.Empty;
    }
}
