using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class EditProfilePage : ContentPage
    {
        private int _userId;
        private int _selectedAvatarId;

        public EditProfilePage()
        {
            InitializeComponent();
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }

        private void RenderAvatars()
        {
            BindableLayout.SetItemsSource(AvatarsLayout, AvatarCatalog.Options(_selectedAvatarId));
            var avatar = AvatarCatalog.Get(_selectedAvatarId);
            PreviewLabel.Text = avatar.Emoji;
            PreviewFrame.BackgroundColor = avatar.Color;
        }

        private void OnAvatarTapped(object sender, EventArgs e)
        {
            var option = (sender as Frame)?.BindingContext as AvatarOption;
            if (option == null) return;

            _selectedAvatarId = option.Id;
            RenderAvatars();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _userId = Preferences.Get("CurrentUserId", 0);

            using var context = new AppDbContext();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == _userId);

            if (user != null)
            {
                UsernameEntry.Text = user.Username;
                EmailEntry.Text = user.Email;
                _selectedAvatarId = user.AvatarId;
                RenderAvatars();
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            string newUsername = UsernameEntry.Text?.Trim() ?? "";
            string newEmail = EmailEntry.Text?.Trim() ?? "";
            string newPassword = NewPasswordEntry.Text ?? "";

            if (newUsername.Length == 0 || newEmail.Length == 0)
            {
                ShowError("Kullanýcý adý ve e-posta boþ olamaz.");
                return;
            }
            if (newUsername.Length < 3 || newUsername.Contains('@'))
            {
                ShowError("Kullanýcý adý en az 3 karakter olmalý ve @ içeremez.");
                return;
            }

            if (!System.Net.Mail.MailAddress.TryCreate(newEmail, out var parsed) || parsed.Address != newEmail)
            {
                ShowError("Geçerli bir e-posta adresi gir.");
                return;
            }

            try
            {
                using var context = new AppDbContext();
                var user = await context.Users.FirstOrDefaultAsync(u => u.Id == _userId);
                if (user == null) return;

                bool taken = await context.Users.AnyAsync(u =>
                    u.Id != _userId && (u.Username == newUsername || u.Email == newEmail));
                if (taken)
                {
                    ShowError("Bu kullanýcý adý veya e-posta baþka biri tarafýndan kullanýlýyor.");
                    return;
                }

                if (newPassword.Length > 0)
                {
                    if (newPassword.Length < 8)
                    {
                        ShowError("Yeni þifre en az 8 karakter olmalý.");
                        return;
                    }

                    if (string.IsNullOrEmpty(CurrentPasswordEntry.Text) ||
                        !BCrypt.Net.BCrypt.Verify(CurrentPasswordEntry.Text, user.PasswordHash))
                    {
                        ShowError("Mevcut þifren hatalý.");
                        return;
                    }

                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

                user.Username = newUsername;
                user.Email = newEmail;
                user.AvatarId = Math.Clamp(_selectedAvatarId, 0, AvatarCatalog.Count - 1);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ShowError("Bu kullanýcý adý veya e-posta baþka biri tarafýndan kullanýlýyor.");
                return;
            }

            CurrentPasswordEntry.Text = string.Empty;
            NewPasswordEntry.Text = string.Empty;
            ErrorLabel.IsVisible = false;

            await DisplayAlert("Baþarýlý", "Profilin güncellendi!", "Tamam");
            await Navigation.PopAsync();
        }
    }
}