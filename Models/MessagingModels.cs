using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planoria.Models
{
    public enum ConversationStatus { Pending, Accepted, Declined }
    public enum InviteStatus { Pending, Accepted, Declined }

    public class DirectConversation
    {
        [Key]
        public int Id { get; set; }

        public int InitiatorId { get; set; }
        [ForeignKey(nameof(InitiatorId))]
        public User? Initiator { get; set; }

        public int RecipientId { get; set; }
        [ForeignKey(nameof(RecipientId))]
        public User? Recipient { get; set; }

        public ConversationStatus Status { get; set; } = ConversationStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<DirectMessage> Messages { get; set; } = new();
    }

    public class DirectMessage
    {
        [Key]
        public int Id { get; set; }

        public int ConversationId { get; set; }
        [ForeignKey(nameof(ConversationId))]
        public DirectConversation? Conversation { get; set; }

        public int SenderId { get; set; }
        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = "";

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }

    public class GroupInvite
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }
        [ForeignKey(nameof(GroupId))]
        public Group? Group { get; set; }

        public int InvitedUserId { get; set; }
        [ForeignKey(nameof(InvitedUserId))]
        public User? InvitedUser { get; set; }

        public int InvitedByUserId { get; set; }
        [ForeignKey(nameof(InvitedByUserId))]
        public User? InvitedBy { get; set; }

        public InviteStatus Status { get; set; } = InviteStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}