using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planoria.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public Group? Group { get; set; }

        public int SenderId { get; set; }

        [ForeignKey(nameof(SenderId))]
        public User? Sender { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}