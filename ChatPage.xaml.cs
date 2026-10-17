using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class ChatPage : ContentPage
    {
        private readonly ObservableCollection<ChatListItem> _chats = new();

        public ChatPage()
        {
            InitializeComponent();
            ChatsCollectionView.ItemsSource = _chats;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadChats();
        }

        private async Task LoadChats()
        {
            int me = Preferences.Get("CurrentUserId", 0);
            using var context = new AppDbContext();

            var blockedIds = await context.FriendRequests
                .Where(fr => (fr.SenderId == me || fr.ReceiverId == me)
                             && fr.Status == FriendRequestStatus.Blocked)
                .Select(fr => fr.SenderId == me ? fr.ReceiverId : fr.SenderId)
                .ToListAsync();

            var items = new List<ChatListItem>();

            // Gruplar
            var groupIds = await context.GroupMembers
                .Where(gm => gm.UserId == me)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            var groups = await context.Groups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
            foreach (var g in groups)
            {
                int count = await context.GroupMembers.CountAsync(gm => gm.GroupId == g.Id);
                var last = await context.ChatMessages
                    .Where(m => m.GroupId == g.Id)
                    .Include(m => m.Sender)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                items.Add(new ChatListItem
                {
                    IsGroup = true,
                    TargetId = g.Id,
                    Name = g.Name,
                    Subtitle = last == null
                        ? $"{count} üye"
                        : $"{(last.SenderId == me ? "Sen" : last.Sender?.Username ?? "?")}: {ChatListItem.Preview(last.Content)}",
                    SortTime = last?.SentAt ?? g.CreatedAt
                });
            }

            // Birebir sohbetler: kabul edilmiþler + benim gönderip yanýt beklediðim istekler
            var convs = await context.DirectConversations
                .Where(c => (c.InitiatorId == me || c.RecipientId == me)
                            && c.Messages.Any()
                            && (c.Status == ConversationStatus.Accepted
                                || (c.Status == ConversationStatus.Pending && c.InitiatorId == me)))
                .Include(c => c.Initiator)
                .Include(c => c.Recipient)
                .ToListAsync();

            foreach (var c in convs)
            {
                var other = c.InitiatorId == me ? c.Recipient : c.Initiator;
                if (other == null || blockedIds.Contains(other.Id)) continue;

                var last = await context.DirectMessages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                string subtitle = last == null
                    ? ""
                    : (last.SenderId == me ? "Sen: " : "") + ChatListItem.Preview(last.Content);
                if (c.Status == ConversationStatus.Pending)
                    subtitle = "? Ýstek gönderildi · " + subtitle;

                items.Add(new ChatListItem
                {
                    IsGroup = false,
                    TargetId = c.Id,
                    Name = other.Username,
                    AvatarId = other.AvatarId,
                    Subtitle = subtitle,
                    SortTime = last?.SentAt ?? c.CreatedAt
                });
            }

            _chats.Clear();
            foreach (var item in items.OrderByDescending(i => i.SortTime))
                _chats.Add(item);

            // Bekleyen istek sayýsý
            int msgRequests = await context.DirectConversations.CountAsync(c =>
                c.RecipientId == me
                && c.Status == ConversationStatus.Pending
                && c.Messages.Any()
                && !blockedIds.Contains(c.InitiatorId));

            int groupInvites = await context.GroupInvites.CountAsync(i =>
                i.InvitedUserId == me
                && i.Status == InviteStatus.Pending
                && !blockedIds.Contains(i.InvitedByUserId));

            int total = msgRequests + groupInvites;
            RequestsButton.Text = total > 0 ? $"?? Ýstekler ({total})" : "?? Ýstekler";
            RequestsButton.BackgroundColor = total > 0 ? Color.FromArgb("#D98BA8") : Colors.White;
            RequestsButton.TextColor = total > 0 ? Colors.White : Color.FromArgb("#D98BA8");
        }

        private async void OnChatTapped(object sender, EventArgs e)
        {
            var item = (sender as Frame)?.BindingContext as ChatListItem;
            if (item == null) return;

            if (item.IsGroup)
                await Navigation.PushAsync(new GroupChatPage(item.TargetId));
            else
                await Navigation.PushAsync(new DirectChatPage(item.TargetId));
        }

        private async void OnRequestsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RequestsPage());
        }

        private async void OnNewChatClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SelectPeoplePage(SelectPeopleMode.NewChat));
        }

        private async void OnCreateGroupClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SelectPeoplePage(SelectPeopleMode.CreateGroup));
        }
    }
}