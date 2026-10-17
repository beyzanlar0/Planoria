using Microsoft.Maui.Graphics;

namespace Planoria.Models
{
    public class FriendListItem
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string ActionText { get; set; } = "";
        public string ActionType { get; set; } = "";
        public int? FriendRequestId { get; set; }
        public int AvatarId { get; set; }

        public string AvatarEmoji => AvatarCatalog.Get(AvatarId).Emoji;
        public Color AvatarColor => AvatarCatalog.Get(AvatarId).Color;
    }
}