using Microsoft.Maui.Graphics;

namespace Planoria.Models
{
    public class AvatarOption
    {
        public int Id { get; set; }
        public string Emoji { get; set; } = "";
        public Color BackColor { get; set; } = Colors.Pink;
        public bool IsSelected { get; set; }

        public Color Border => IsSelected ? Color.FromArgb("#D98BA8") : Color.FromArgb("#F4C2D7");
        public double Opacity => IsSelected ? 1.0 : 0.55;
    }

    public static class AvatarCatalog
    {
        private static readonly (string Emoji, string Hex)[] Items =
        {
            ("🐱", "#F4A6C1"), ("🐶", "#F6C99A"), ("🐰", "#E9C3F0"), ("🦊", "#F7B28B"),
            ("🐻", "#D9B99B"), ("🐼", "#CFE3D4"), ("🐸", "#B5E0B0"), ("🐥", "#FBE38E"),
            ("🦄", "#D5C6F5"), ("🐙", "#F5B1B1"), ("🌸", "#F7C6D9"), ("🍓", "#F4A0A8")
        };

        public static int Count => Items.Length;

        public static (string Emoji, Color Color) Get(int id)
        {
            if (id < 0 || id >= Items.Length) id = 0;
            return (Items[id].Emoji, Color.FromArgb(Items[id].Hex));
        }

        public static List<AvatarOption> Options(int selectedId)
        {
            var list = new List<AvatarOption>();
            for (int i = 0; i < Items.Length; i++)
            {
                list.Add(new AvatarOption
                {
                    Id = i,
                    Emoji = Items[i].Emoji,
                    BackColor = Color.FromArgb(Items[i].Hex),
                    IsSelected = i == selectedId
                });
            }
            return list;
        }
    }
}