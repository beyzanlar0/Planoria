using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planoria.Models
{
    public enum FriendRequestStatus
    {
        Pending,
        Accepted,
        Rejected,
        Blocked
    }

    public class FriendRequest
    {
        [Key]
        public int Id { get; set; }

        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        public int ReceiverId { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public User? Receiver { get; set; }

        public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

        // Engellemeyi yapan kullanıcı (sadece o engeli kaldırabilir)
        public int? BlockedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }
    }
}