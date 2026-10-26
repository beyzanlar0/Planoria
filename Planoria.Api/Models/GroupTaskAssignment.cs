using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planoria.Models
{
    public enum AssignmentStatus
    {
        Requested,   // üye istedi, lider onayı bekliyor
        Active,      // onaylandı, süre işliyor
        Completed,
        Expired,     // süre doldu
        Rejected
    }

    public class GroupTaskAssignment
    {
        [Key]
        public int Id { get; set; }

        public int GroupTaskId { get; set; }

        [ForeignKey(nameof(GroupTaskId))]
        public GroupTask? GroupTask { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public AssignmentStatus Status { get; set; } = AssignmentStatus.Requested;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? DeadlineAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}