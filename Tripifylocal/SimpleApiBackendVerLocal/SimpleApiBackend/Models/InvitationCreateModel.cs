namespace SimpleApiBackend.Models
{
   public class InvitationCreateModel
{
        public int SenderId { get; set; } 
        public string ReceiverUsername { get; set; } 
        public int TripId { get; set; }
    }

}
