using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class GroupChatPage : ContentPage
    {
        private readonly int _groupId;
        private readonly ObservableCollection<ChatMessageItem> _messages = new();

        public GroupChatPage(int groupId)
        {
            InitializeComponent();
            _groupId = groupId;
            MessagesCollectionView.ItemsSource = _messages;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadGroupInfo();
            await LoadMessages();
        }

        private async Task LoadGroupInfo()
        {
            using var context = new AppDbContext();
            var group = await context.Groups.FindAsync(_groupId);
            int count = await context.GroupMembers.CountAsync(gm => gm.GroupId == _groupId);

            if (group != null)
            {
                GroupNameLabel.Text = group.Name;
                MemberCountLabel.Text = $"{count} üye";
            }
        }

        private async Task LoadMessages()
        {
            int userId = Preferences.Get("CurrentUserId", 0);

            using var context = new AppDbContext();
            bool isMember = await context.GroupMembers
    .AnyAsync(gm => gm.GroupId == _groupId && gm.UserId == userId);
            if (!isMember)
            {
                await Navigation.PopToRootAsync();
                return;
            }
            var messages = await context.ChatMessages
                .Where(m => m.GroupId == _groupId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            _messages.Clear();
            foreach (var m in messages)
            {
                var avatar = AvatarCatalog.Get(m.Sender?.AvatarId ?? 0);
                bool mine = m.SenderId == userId;

                _messages.Add(new ChatMessageItem
                {
                    SenderName = m.Sender?.Username ?? "?",
                    Content = m.Content,
                    TimeText = m.SentAt.ToLocalTime().ToString("HH:mm"),
                    IsMine = mine,
                    ShowSenderName = !mine,
                    AvatarEmoji = avatar.Emoji,
                    AvatarColor = avatar.Color
                });
            }

            if (_messages.Any())
                MessagesCollectionView.ScrollTo(_messages.Last(), animate: false);
        }

        private async void OnSendClicked(object sender, EventArgs e)
        {
            string text = MessageEntry.Text;
            if (string.IsNullOrWhiteSpace(text)) return;

            int userId = Preferences.Get("CurrentUserId", 0);

            using (var context = new AppDbContext())
            {
                bool isMember = await context.GroupMembers
                    .AnyAsync(gm => gm.GroupId == _groupId && gm.UserId == userId);
                if (!isMember)
                {
                    await DisplayAlert("Uyarý", "Bu grubun üyesi deðilsin.", "Tamam");
                    await Navigation.PopToRootAsync();
                    return;
                }

                context.ChatMessages.Add(new ChatMessage
                {
                    GroupId = _groupId,
                    SenderId = userId,
                    Content = text.Trim()
                });
                await context.SaveChangesAsync();
            }

            MessageEntry.Text = string.Empty;
            await LoadMessages();
        }

        private async void OnTasksClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GroupTasksPage(_groupId));
        }

        private async void OnHeaderTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GroupInfoPage(_groupId));
        }
    }
}