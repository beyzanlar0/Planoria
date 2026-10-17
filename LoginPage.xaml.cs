using Microsoft.EntityFrameworkCore;
using Planoria.Data;

namespace Planoria
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string emailOrUsername = EmailEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text ?? "";

            if (emailOrUsername.Length == 0 || password.Length == 0)
            {
                ErrorLabel.Text = "Lütfen tüm alanlarý doldurun.";
                ErrorLabel.IsVisible = true;
                return;
            }

            using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u =>
                u.Username == emailOrUsername || u.Email == emailOrUsername);

            // Hangi bilginin yanlýþ olduðunu belli etmeyen tek mesaj
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ErrorLabel.Text = "Kullanýcý adý/e-posta veya þifre hatalý.";
                ErrorLabel.IsVisible = true;
                return;
            }

            Preferences.Set("CurrentUserId", user.Id);
            Application.Current.MainPage = new AppShell();
        }

        private async void OnGoToRegisterTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}