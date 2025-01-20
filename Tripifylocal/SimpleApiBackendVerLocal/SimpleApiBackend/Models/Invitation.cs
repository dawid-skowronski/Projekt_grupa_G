using System;
using System.ComponentModel.DataAnnotations;

namespace SimpleApiBackend.Models
{
    public class Invitation
    {
        [Key]
        public int InvitationId { get; set; }

        [Required]
        public int TripId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Domyślnie 'Pending'

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        
        public Trip Trip { get; set; }
        public User Sender { get; set; }
        public User Receiver { get; set; }
    }
}
