using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class ProfilePage : ContentPage
    {
        public ProfilePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            int userId = Preferences.Get("CurrentUserId", 0);

            using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                UsernameLabel.Text = user.Username;
                EmailLabel.Text = user.Email;

                var avatar = AvatarCatalog.Get(user.AvatarId);
                AvatarLabel.Text = avatar.Emoji;
                AvatarFrame.BackgroundColor = avatar.Color;
            }
        }

        private async void OnManageClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new EditProfilePage());
        }

        private async void OnBlockedUsersClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new BlockedUsersPage());
        }

        private void OnLogoutClicked(object sender, EventArgs e)
        {
            Preferences.Remove("CurrentUserId");
            Application.Current!.MainPage = new NavigationPage(new LoginPage());
        }
    }
}