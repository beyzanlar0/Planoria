using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Planoria.Models
{
    public class GroupListItem
    {
        public int GroupId { get; set; }
        public string Name { get; set; }
        public int MemberCount { get; set; }
        public string MemberCountText => $"{MemberCount} üye";
    }
}