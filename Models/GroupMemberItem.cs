using Microsoft.Maui.Graphics;

namespace Planoria.Models
{
    public class GroupMemberItem
    {
        public int GroupMemberId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public GroupRole Role { get; set; }
        public bool IsMe { get; set; }
        public int AvatarId { get; set; }

        public string AvatarEmoji => AvatarCatalog.Get(AvatarId).Emoji;
        public Color AvatarColor => AvatarCatalog.Get(AvatarId).Color;

        public string DisplayName => IsMe ? $"{Username} (sen)" : Username;

        public string RoleText => Role switch
        {
            GroupRole.Owner => "👑 Yönetici",
            GroupRole.Admin => "⭐ Yardımcı yönetici",
            _ => "Üye"
        };
    }
}