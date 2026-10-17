using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planoria.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int AvatarId { get; set; } = 0;
        // Navigation properties (ilişkiler)
        public List<TaskItem> Tasks { get; set; } = new();
        public List<FriendRequest> SentFriendRequests { get; set; } = new();
        public List<FriendRequest> ReceivedFriendRequests { get; set; } = new();
    }
}
