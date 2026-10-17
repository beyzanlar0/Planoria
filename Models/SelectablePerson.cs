using Microsoft.Maui.Graphics;

namespace Planoria.Models
{
    public class SelectablePerson
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Tag { get; set; } = "";
        public bool IsSelected { get; set; }
        public bool IsFriend { get; set; }
        public bool ShowCheckbox { get; set; } = true;
        public int AvatarId { get; set; }

        public string AvatarEmoji => AvatarCatalog.Get(AvatarId).Emoji;
        public Color AvatarColor => AvatarCatalog.Get(AvatarId).Color;
    }
}