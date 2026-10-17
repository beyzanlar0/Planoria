using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class MemberProfilePage : ContentPage
    {
        private readonly int _userId;

        public MemberProfilePage(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            using var context = new AppDbContext();
            var user = await context.Users.FindAsync(_userId);
            if (user == null) return;

            UsernameLabel.Text = user.Username;
            JoinedLabel.Text = $"Planoria'ya katýldý: {user.CreatedAt.ToLocalTime():dd MMMM yyyy}";

            var avatar = AvatarCatalog.Get(user.AvatarId);
            AvatarLabel.Text = avatar.Emoji;
            AvatarFrame.BackgroundColor = avatar.Color;
        }
    }
}