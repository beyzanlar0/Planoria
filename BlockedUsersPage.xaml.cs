using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class BlockedUsersPage : ContentPage
    {
        private readonly ObservableCollection<FriendListItem> _blocked = new();

        public BlockedUsersPage()
        {
            InitializeComponent();
            BlockedCollectionView.ItemsSource = _blocked;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            int userId = Preferences.Get("CurrentUserId", 0);

            using var context = new AppDbContext();
            var blockedList = await context.FriendRequests
                .Where(fr => (fr.SenderId == userId || fr.ReceiverId == userId)
                             && fr.Status == FriendRequestStatus.Blocked
                             && fr.BlockedByUserId == userId)
                .Include(fr => fr.Sender)
                .Include(fr => fr.Receiver)
                .ToListAsync();

            _blocked.Clear();
            foreach (var fr in blockedList)
            {
                var other = fr.SenderId == userId ? fr.Receiver : fr.Sender;
                if (other == null) continue;

                _blocked.Add(new FriendListItem
                {
                    UserId = other.Id,
                    Username = other.Username,
                    FriendRequestId = fr.Id
                });
            }
        }

        private async void OnUnblockClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as FriendListItem;
            if (item == null) return;

            int userId = Preferences.Get("CurrentUserId", 0);

            using (var context = new AppDbContext())
            {
                // Sadece engeli koyan kiþi kaldýrabilir
                var request = await context.FriendRequests.FirstOrDefaultAsync(fr =>
                    fr.Id == item.FriendRequestId && fr.BlockedByUserId == userId);

                if (request != null)
                {
                    context.FriendRequests.Remove(request);
                    await context.SaveChangesAsync();
                }
            }

            var toRemove = _blocked.FirstOrDefault(x => x.FriendRequestId == item.FriendRequestId);
            if (toRemove != null) _blocked.Remove(toRemove);
        }
    }
}