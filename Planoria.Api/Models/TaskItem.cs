using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planoria.Models
{

    public enum RecurrenceType
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public class TaskItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public RecurrenceType RecurrenceType { get; set; }

        public DateTime DueDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime? CompletedAt { get; set; }

        // Bu görevi oluşturan/sahip olan kullanıcı
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        // Grup göreviyse hangi gruba ait (bireyselse null kalır)
        public int? GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public Group? Group { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
