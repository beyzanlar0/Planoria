using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private void ShowError(string message)
        {
            ErrorLabel.TextColor = Color.FromArgb("#E0607A");
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text?.Trim() ?? "";
            string email = EmailEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text ?? "";

            if (username.Length == 0 || email.Length == 0 || password.Length == 0)
            {
                ShowError("Lütfen tüm alanlarý doldurun.");
                return;
            }
            if (username.Length < 3 || username.Contains('@'))
            {
                ShowError("Kullanýcý adý en az 3 karakter olmalý ve @ içeremez.");
                return;
            }
            if (password.Length < 8)
            {
                ShowError("Þifre en az 8 karakter olmalý.");
                return;
            }

            if (!System.Net.Mail.MailAddress.TryCreate(email, out var parsed) || parsed.Address != email)
            {
                ShowError("Geçerli bir e-posta adresi gir.");
                return;
            }

            try
            {
                using var context = new AppDbContext();

                bool exists = await context.Users.AnyAsync(u => u.Username == username || u.Email == email);
                if (exists)
                {
                    ShowError("Bu kullanýcý adý veya e-posta zaten kullanýlýyor.");
                    return;
                }

                context.Users.Add(new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
                });
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Ayný anda baþka biri ayný bilgiyle kayýt olduysa benzersiz indeks yakalar
                ShowError("Bu kullanýcý adý veya e-posta zaten kullanýlýyor.");
                return;
            }

            PasswordEntry.Text = string.Empty;
            ErrorLabel.TextColor = Colors.Green;
            ErrorLabel.Text = "Kayýt baþarýlý! Giriþ yapabilirsin.";
            ErrorLabel.IsVisible = true;
        }

        private async void OnGoToLoginTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage());
        }
    }
}   