using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class RequestsPage : ContentPage
    {
        private readonly int _me;
        private readonly ObservableCollection<RequestItem> _items = new();

        public RequestsPage()
        {
            InitializeComponent();
            _me = Preferences.Get("CurrentUserId", 0);
            RequestsCollectionView.ItemsSource = _items;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAll();
        }

        private async Task LoadAll()
        {
            using var context = new AppDbContext();

            // Engellediðim ya da beni engelleyenlerin istekleri görünmesin
            var blockedIds = await context.FriendRequests
                .Where(fr => (fr.SenderId == _me || fr.ReceiverId == _me)
                             && fr.Status == FriendRequestStatus.Blocked)
                .Select(fr => fr.SenderId == _me ? fr.ReceiverId : fr.SenderId)
                .ToListAsync();

            var list = new List<RequestItem>();

            var convs = await context.DirectConversations
                .Where(c => c.RecipientId == _me
                            && c.Status == ConversationStatus.Pending
                            && c.Messages.Any()
                            && !blockedIds.Contains(c.InitiatorId))
                .Include(c => c.Initiator)
                .ToListAsync();

            foreach (var c in convs)
            {
                var first = await context.DirectMessages
                    .Where(m => m.ConversationId == c.Id)
                    .OrderBy(m => m.SentAt)
                    .FirstOrDefaultAsync();

                list.Add(new RequestItem
                {
                    IsGroupInvite = false,
                    RefId = c.Id,
                    OtherUserId = c.InitiatorId,
                    Title = c.Initiator?.Username ?? "?",
                    AvatarId = c.Initiator?.AvatarId ?? 0,
                    Subtitle = first == null ? "" : $"\"{ChatListItem.Preview(first.Content)}\"",
                    CreatedAt = first?.SentAt ?? c.CreatedAt
                });
            }

            var invites = await context.GroupInvites
                .Where(i => i.InvitedUserId == _me
                            && i.Status == InviteStatus.Pending
                            && !blockedIds.Contains(i.InvitedByUserId))
                .Include(i => i.Group)
                .Include(i => i.InvitedBy)
                .ToListAsync();

            foreach (var i in invites)
            {
                list.Add(new RequestItem
                {
                    IsGroupInvite = true,
                    RefId = i.Id,
                    OtherUserId = i.InvitedByUserId,
                    Title = i.Group?.Name ?? "Grup",
                    Subtitle = $"{i.InvitedBy?.Username ?? "?"} seni bu gruba eklemek istiyor",
                    CreatedAt = i.CreatedAt
                });
            }

            _items.Clear();
            foreach (var item in list.OrderByDescending(x => x.CreatedAt))
                _items.Add(item);
        }

        private async void OnRequestTapped(object sender, EventArgs e)
        {
            var item = (sender as Frame)?.BindingContext as RequestItem;
            if (item == null) return;

            // Mesaj isteðine dokununca konuþmayý açýp mesajý okuyabilirsin
            if (!item.IsGroupInvite)
                await Navigation.PushAsync(new DirectChatPage(item.RefId));
        }

        private async void OnAcceptClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as RequestItem;
            if (item == null) return;

            using (var context = new AppDbContext())
            {
                if (item.IsGroupInvite)
                {
                    var invite = await context.GroupInvites
                        .FirstOrDefaultAsync(i => i.Id == item.RefId && i.InvitedUserId == _me);

                    if (invite != null && invite.Status == InviteStatus.Pending)
                    {
                        bool already = await context.GroupMembers
                            .AnyAsync(gm => gm.GroupId == invite.GroupId && gm.UserId == _me);
                        if (!already)
                        {
                            context.GroupMembers.Add(new GroupMember
                            {
                                GroupId = invite.GroupId,
                                UserId = _me,
                                Role = GroupRole.Member
                            });
                        }
                        invite.Status = InviteStatus.Accepted;
                        await context.SaveChangesAsync();
                    }
                }
                else
                {
                    var conv = await context.DirectConversations
                        .FirstOrDefaultAsync(c => c.Id == item.RefId && c.RecipientId == _me);
                    if (conv != null)
                    {
                        conv.Status = ConversationStatus.Accepted;
                        await context.SaveChangesAsync();
                    }
                }
            }

            await LoadAll();
        }

        private async void OnDeclineClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as RequestItem;
            if (item == null) return;

            using (var context = new AppDbContext())
            {
                if (item.IsGroupInvite)
                {
                    var invite = await context.GroupInvites
                        .FirstOrDefaultAsync(i => i.Id == item.RefId && i.InvitedUserId == _me);
                    if (invite != null)
                    {
                        invite.Status = InviteStatus.Declined;
                        await context.SaveChangesAsync();
                    }
                }
                else
                {
                    var conv = await context.DirectConversations
                        .FirstOrDefaultAsync(c => c.Id == item.RefId && c.RecipientId == _me);
                    if (conv != null)
                    {
                        conv.Status = ConversationStatus.Declined;
                        await context.SaveChangesAsync();
                    }
                }
            }

            await LoadAll();
        }
    }
}