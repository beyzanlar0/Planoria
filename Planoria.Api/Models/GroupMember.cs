using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planoria.Models
{

 
    public enum GroupRole
    {
        Owner,
        Admin,
        Member
    }
    public class GroupMember
    {
        [Key]
        public int Id { get; set; }

        public int GroupId { get; set; }

        [ForeignKey(nameof(GroupId))]
        public Group? Group { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        public GroupRole Role { get; set; } = GroupRole.Member;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
