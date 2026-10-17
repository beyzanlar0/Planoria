using Microsoft.Maui.Graphics;

namespace Planoria.Models
{
    public class ChatMessageItem
    {
        public string SenderName { get; set; } = "";
        public string Content { get; set; } = "";
        public string TimeText { get; set; } = "";
        public bool IsMine { get; set; }
        public bool ShowSenderName { get; set; }
        public string AvatarEmoji { get; set; } = "🐱";
        public Color AvatarColor { get; set; } = Color.FromArgb("#F4A6C1");
    }
}