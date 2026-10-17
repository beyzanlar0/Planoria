using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class FriendsPage : ContentPage
    {
        private readonly ObservableCollection<FriendListItem> _items = new();
        private readonly ObservableCollection<FriendListItem> _friends = new();

        public FriendsPage()
        {
            InitializeComponent();
            ResultsCollectionView.ItemsSource = _items;
            FriendsCollectionView.ItemsSource = _friends;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPendingRequests();
            await LoadFriends();
        }

        // Bana gelen bekleyen arkadaþlýk istekleri
        private async Task LoadPendingRequests()
        {
            int me = Preferences.Get("CurrentUserId", 0);

            using var context = new AppDbContext();
            var pending = await context.FriendRequests
                .Where(fr => fr.ReceiverId == me && fr.Status == FriendRequestStatus.Pending)
                .Include(fr => fr.Sender)
                .ToListAsync();

            _items.Clear();
            PendingRequestsLabel.IsVisible = pending.Any();

            foreach (var req in pending)
            {
                if (req.Sender == null) continue;

                _items.Add(new FriendListItem
                {
                    UserId = req.SenderId,
                    Username = req.Sender.Username,
                    AvatarId = req.Sender.AvatarId,
                    ActionText = "Kabul Et",
                    ActionType = "Accept",
                    FriendRequestId = req.Id
                });
            }
        }

        // Kabul edilmiþ arkadaþlarým
        private async Task LoadFriends()
        {
            int me = Preferences.Get("CurrentUserId", 0);

            using var context = new AppDbContext();
            var accepted = await context.FriendRequests
                .Where(fr => (fr.SenderId == me || fr.ReceiverId == me)
                             && fr.Status == FriendRequestStatus.Accepted)
                .Include(fr => fr.Sender)
                .Include(fr => fr.Receiver)
                .ToListAsync();

            _friends.Clear();
            foreach (var fr in accepted)
            {
                var friend = fr.SenderId == me ? fr.Receiver : fr.Sender;
                if (friend == null) continue;

                _friends.Add(new FriendListItem
                {
                    UserId = friend.Id,
                    Username = friend.Username,
                    AvatarId = friend.AvatarId,
                    FriendRequestId = fr.Id
                });
            }
        }

        private async void OnSearchClicked(object sender, EventArgs e)
        {
            await RunSearch();
        }

        private async Task RunSearch()
        {
            string searchText = SearchEntry.Text?.Trim() ?? "";

            if (searchText.Length == 0)
            {
                await LoadPendingRequests();
                return;
            }

            int me = Preferences.Get("CurrentUserId", 0);

            using var context = new AppDbContext();

            // Engelli iliþkiler (hangi taraf engellemiþ olursa olsun) sonuçlarda görünmez
            var blockedIds = await context.FriendRequests
                .Where(fr => (fr.SenderId == me || fr.ReceiverId == me)
                             && fr.Status == FriendRequestStatus.Blocked)
                .Select(fr => fr.SenderId == me ? fr.ReceiverId : fr.SenderId)
                .ToListAsync();

            var foundUsers = await context.Users
                .Where(u => u.Username.Contains(searchText)
                            && u.Id != me
                            && !blockedIds.Contains(u.Id))
                .Take(20)
                .ToListAsync();

            var existingRequests = await context.FriendRequests
                .Where(fr => fr.SenderId == me || fr.ReceiverId == me)
                .ToListAsync();

            _items.Clear();
            PendingRequestsLabel.IsVisible = false;

            foreach (var user in foundUsers)
            {
                var existing = existingRequests.FirstOrDefault(fr =>
                    fr.SenderId == user.Id || fr.ReceiverId == user.Id);

                string actionText = "Arkadaþ Ekle";
                string actionType = "AddFriend";
                int? requestId = null;

                if (existing != null)
                {
                    if (existing.Status == FriendRequestStatus.Accepted)
                    {
                        actionText = "Arkadaþsýnýz";
                        actionType = "None";
                    }
                    else if (existing.Status == FriendRequestStatus.Pending)
                    {
                        if (existing.ReceiverId == me)
                        {
                            // O bana istek göndermiþ, buradan da kabul edebilirim
                            actionText = "Kabul Et";
                            actionType = "Accept";
                            requestId = existing.Id;
                        }
                        else
                        {
                            actionText = "Beklemede";
                            actionType = "None";
                        }
                    }
                    else
                    {
                        actionText = "Beklemede";
                        actionType = "None";
                    }
                }

                _items.Add(new FriendListItem
                {
                    UserId = user.Id,
                    Username = user.Username,
                    AvatarId = user.AvatarId,
                    ActionText = actionText,
                    ActionType = actionType,
                    FriendRequestId = requestId
                });
            }
        }

        private async void OnActionButtonClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as FriendListItem;
            if (item == null) return;

            int me = Preferences.Get("CurrentUserId", 0);

            if (item.ActionType == "AddFriend")
            {
                bool sent = false;

                using (var context = new AppDbContext())
                {
                    bool alreadyExists = await context.FriendRequests.AnyAsync(fr =>
                        (fr.SenderId == me && fr.ReceiverId == item.UserId) ||
                        (fr.SenderId == item.UserId && fr.ReceiverId == me));

                    if (!alreadyExists && item.UserId != me)
                    {
                        context.FriendRequests.Add(new FriendRequest
                        {
                            SenderId = me,
                            ReceiverId = item.UserId,
                            Status = FriendRequestStatus.Pending
                        });
                        await context.SaveChangesAsync();
                        sent = true;
                    }
                }

                await DisplayAlert(sent ? "Baþarýlý" : "Bilgi",
                    sent ? "Arkadaþlýk isteði gönderildi!" : "Bu kullanýcýyla zaten bir kaydýnýz var.",
                    "Tamam");

                await RunSearch();
            }
            else if (item.ActionType == "Accept")
            {
                using (var context = new AppDbContext())
                {
                    // Sadece bana gelen ve bekleyen istek kabul edilebilir
                    var request = await context.FriendRequests.FirstOrDefaultAsync(r =>
                        r.Id == item.FriendRequestId
                        && r.ReceiverId == me
                        && r.Status == FriendRequestStatus.Pending);

                    if (request != null)
                    {
                        request.Status = FriendRequestStatus.Accepted;
                        request.RespondedAt = DateTime.UtcNow;
                        await context.SaveChangesAsync();
                    }
                }

                if (string.IsNullOrWhiteSpace(SearchEntry.Text))
                    await LoadPendingRequests();
                else
                    await RunSearch();

                await LoadFriends();
            }
        }

        private async void OnChatWithFriendClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as FriendListItem;
            if (item == null) return;

            int me = Preferences.Get("CurrentUserId", 0);
            var (conversationId, error) = await DirectChatService.OpenOrCreateAsync(me, item.UserId);

            if (conversationId == null)
            {
                await DisplayAlert("Uyarý", error ?? "Sohbet açýlamadý.", "Tamam");
                return;
            }

            await Navigation.PushAsync(new DirectChatPage(conversationId.Value));
        }

        private async void OnBlockFriendClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as FriendListItem;
            if (item == null) return;

            bool confirm = await DisplayAlert("Engelle",
                $"{item.Username} kullanýcýsýný engellemek istediðine emin misin?", "Evet", "Vazgeç");
            if (!confirm) return;

            int me = Preferences.Get("CurrentUserId", 0);

            using (var context = new AppDbContext())
            {
                // Sadece kendi arkadaþlýk kaydýmý engelleyebilirim
                var request = await context.FriendRequests.FirstOrDefaultAsync(r =>
                    r.Id == item.FriendRequestId && (r.SenderId == me || r.ReceiverId == me));

                if (request != null)
                {
                    request.Status = FriendRequestStatus.Blocked;
                    request.BlockedByUserId = me;
                    await context.SaveChangesAsync();
                }
            }

            await LoadFriends();
        }

        private async void OnRemoveFriendClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as FriendListItem;
            if (item == null) return;

            bool confirm = await DisplayAlert("Sil",
                $"{item.Username} arkadaþlýktan çýkarýlsýn mý?", "Evet", "Vazgeç");
            if (!confirm) return;

            int me = Preferences.Get("CurrentUserId", 0);

            using (var context = new AppDbContext())
            {
                var request = await context.FriendRequests.FirstOrDefaultAsync(r =>
                    r.Id == item.FriendRequestId && (r.SenderId == me || r.ReceiverId == me));

                if (request != null)
                {
                    context.FriendRequests.Remove(request);
                    await context.SaveChangesAsync();
                }
            }

            await LoadFriends();
        }
    }
}