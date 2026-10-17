using Microsoft.Maui.Graphics;

namespace Planoria.Models
{
    public class ChatListItem
    {
        public bool IsGroup { get; set; }
        public int TargetId { get; set; }   // GroupId veya ConversationId
        public string Name { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public DateTime SortTime { get; set; }
        public int AvatarId { get; set; }

        public string Icon => IsGroup ? "👥" : AvatarCatalog.Get(AvatarId).Emoji;
        public Color IconColor => IsGroup ? Color.FromArgb("#F4A6C1") : AvatarCatalog.Get(AvatarId).Color;

        public string TimeText
        {
            get
            {
                if (SortTime == default) return "";
                var local = SortTime.ToLocalTime();
                return local.Date == DateTime.Today ? local.ToString("HH:mm") : local.ToString("dd.MM");
            }
        }

        public static string Preview(string text) =>
            text.Length <= 40 ? text : text.Substring(0, 40) + "…";
    }

    public class RequestItem
    {
        public bool IsGroupInvite { get; set; }
        public int RefId { get; set; }      // ConversationId veya InviteId
        public int OtherUserId { get; set; }
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public int AvatarId { get; set; }

        public string Icon => IsGroupInvite ? "👥" : AvatarCatalog.Get(AvatarId).Emoji;
        public Color IconColor => IsGroupInvite ? Color.FromArgb("#F4A6C1") : AvatarCatalog.Get(AvatarId).Color;
    }
}