using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class SelectPeoplePage : ContentPage
    {
        private enum AddResult { Added, Invited, Skipped }

        private readonly SelectPeopleMode _mode;
        private readonly int _groupId;
        private readonly int _currentUserId;
        private readonly ObservableCollection<SelectablePerson> _people = new();
        private readonly List<int> _selectedUserIds = new();

        public SelectPeoplePage(SelectPeopleMode mode, int groupId = 0)
        {
            InitializeComponent();
            _mode = mode;
            _groupId = groupId;
            _currentUserId = Preferences.Get("CurrentUserId", 0);
            PeopleCollectionView.ItemsSource = _people;

            switch (_mode)
            {
                case SelectPeopleMode.CreateGroup:
                    PageTitleLabel.Text = "Grup Oluþtur";
                    GroupNameFrame.IsVisible = true;
                    ConfirmButton.Text = "Grubu Oluþtur";
                    break;
                case SelectPeopleMode.AddToGroup:
                    PageTitleLabel.Text = "Üye Ekle";
                    ConfirmButton.Text = "Gruba Ekle";
                    break;
                default:
                    PageTitleLabel.Text = "Yeni Sohbet";
                    ConfirmButton.IsVisible = false;   // kiþiye dokununca sohbet açýlýr
                    break;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (string.IsNullOrWhiteSpace(SearchEntry.Text))
                await LoadFriends();
        }

        private SelectablePerson MakePerson(int id, string username, bool isFriend, int avatarId) => new()
        {
            UserId = id,
            Username = username,
            AvatarId = avatarId,
            IsFriend = isFriend,
            Tag = isFriend
                ? "Arkadaþýn"
                : (_mode == SelectPeopleMode.NewChat ? "Mesaj isteði gönderilecek" : "Davet gönderilecek"),
            ShowCheckbox = _mode != SelectPeopleMode.NewChat,
            IsSelected = _selectedUserIds.Contains(id)
        };

        private async Task LoadFriends()
        {
            using var context = new AppDbContext();

            var accepted = await context.FriendRequests
                .Where(fr => (fr.SenderId == _currentUserId || fr.ReceiverId == _currentUserId)
                             && fr.Status == FriendRequestStatus.Accepted)
                .Include(fr => fr.Sender)
                .Include(fr => fr.Receiver)
                .ToListAsync();

            var list = new List<SelectablePerson>();
            foreach (var fr in accepted)
            {
                var friend = fr.SenderId == _currentUserId ? fr.Receiver : fr.Sender;
                if (friend != null)
                    list.Add(MakePerson(friend.Id, friend.Username, true, friend.AvatarId));
            }

            _people.Clear();
            foreach (var p in list) _people.Add(p);
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = e.NewTextValue?.Trim() ?? "";
            if (searchText.Length == 0)
            {
                await LoadFriends();
                return;
            }

            using var context = new AppDbContext();

            var blockedIds = await context.FriendRequests
                .Where(fr => (fr.SenderId == _currentUserId || fr.ReceiverId == _currentUserId)
                             && fr.Status == FriendRequestStatus.Blocked)
                .Select(fr => fr.SenderId == _currentUserId ? fr.ReceiverId : fr.SenderId)
                .ToListAsync();

            var foundUsers = await context.Users
                .Where(u => u.Username.Contains(searchText)
                            && u.Id != _currentUserId
                            && !blockedIds.Contains(u.Id))
                .Take(20)
                .ToListAsync();

            var friendIds = await context.FriendRequests
                .Where(fr => (fr.SenderId == _currentUserId || fr.ReceiverId == _currentUserId)
                             && fr.Status == FriendRequestStatus.Accepted)
                .Select(fr => fr.SenderId == _currentUserId ? fr.ReceiverId : fr.SenderId)
                .ToListAsync();

            var list = foundUsers
                .Select(u => MakePerson(u.Id, u.Username, friendIds.Contains(u.Id), u.AvatarId))
                .ToList();

            _people.Clear();
            foreach (var p in list) _people.Add(p);
        }

        private void OnPersonCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            var person = (sender as CheckBox)?.BindingContext as SelectablePerson;
            if (person == null) return;

            if (e.Value)
            {
                if (!_selectedUserIds.Contains(person.UserId))
                    _selectedUserIds.Add(person.UserId);
            }
            else
            {
                _selectedUserIds.Remove(person.UserId);
            }
        }

        // Yeni Sohbet modunda kiþiye dokununca sohbeti aç
        private async void OnPersonTapped(object sender, EventArgs e)
        {
            if (_mode != SelectPeopleMode.NewChat) return;

            var person = (sender as Frame)?.BindingContext as SelectablePerson;
            if (person == null) return;

            var (conversationId, error) = await DirectChatService.OpenOrCreateAsync(_currentUserId, person.UserId);
            if (conversationId == null)
            {
                await DisplayAlert("Uyarý", error ?? "Sohbet açýlamadý.", "Tamam");
                return;
            }

            await Navigation.PushAsync(new DirectChatPage(conversationId.Value));
        }

        // Arkadaþsa doðrudan ekler, deðilse davet gönderir
        private async Task<AddResult> AddOrInviteAsync(AppDbContext context, int groupId, int userId)
        {
            if (await context.GroupMembers.AnyAsync(gm => gm.GroupId == groupId && gm.UserId == userId))
                return AddResult.Skipped;

            if (await FriendshipHelper.IsBlockedAsync(context, _currentUserId, userId))
                return AddResult.Skipped;

            var invite = await context.GroupInvites
                .FirstOrDefaultAsync(i => i.GroupId == groupId && i.InvitedUserId == userId);

            if (await FriendshipHelper.AreFriendsAsync(context, _currentUserId, userId))
            {
                context.GroupMembers.Add(new GroupMember
                {
                    GroupId = groupId,
                    UserId = userId,
                    Role = GroupRole.Member
                });
                if (invite != null && invite.Status == InviteStatus.Pending)
                    invite.Status = InviteStatus.Accepted;
                return AddResult.Added;
            }

            if (invite == null)
            {
                context.GroupInvites.Add(new GroupInvite
                {
                    GroupId = groupId,
                    InvitedUserId = userId,
                    InvitedByUserId = _currentUserId
                });
                return AddResult.Invited;
            }

            // Zaten bekleyen ya da reddedilmiþ davet: tekrar gönderilmez (spam önleme)
            if (invite.Status == InviteStatus.Pending || invite.Status == InviteStatus.Declined)
                return AddResult.Skipped;

            // Eskiden kabul edip sonra gruptan çýkmýþ biri: yeniden davet edilebilir
            invite.Status = InviteStatus.Pending;
            invite.InvitedByUserId = _currentUserId;
            invite.CreatedAt = DateTime.UtcNow;
            return AddResult.Invited;
        }

        private async void OnConfirmClicked(object sender, EventArgs e)
        {
            if (!_selectedUserIds.Any())
            {
                await DisplayAlert("Uyarý", "En az bir kiþi seçmelisin.", "Tamam");
                return;
            }

            int added = 0, invited = 0, skipped = 0;
            string message;

            using (var context = new AppDbContext())
            {
                int targetGroupId = _groupId;
                string? groupName = null;

                if (_mode == SelectPeopleMode.AddToGroup)
                {
                    var myRole = await GroupAuth.GetRoleAsync(context, _groupId, _currentUserId);
                    if (myRole == null || myRole == GroupRole.Member)
                    {
                        await DisplayAlert("Uyarý", "Üye eklemek için yönetici olmalýsýn.", "Tamam");
                        return;
                    }
                }
                else if (_mode == SelectPeopleMode.CreateGroup)
                {
                    groupName = GroupNameEntry.Text?.Trim();   // x:Name farklýysa burayý deðiþtir
                    if (string.IsNullOrWhiteSpace(groupName))
                    {
                        await DisplayAlert("Uyarý", "Grup adý yazmalýsýn.", "Tamam");
                        return;
                    }

                    var newGroup = new Group { Name = groupName, CreatedByUserId = _currentUserId };
                    context.Groups.Add(newGroup);
                    await context.SaveChangesAsync();

                    context.GroupMembers.Add(new GroupMember
                    {
                        GroupId = newGroup.Id,
                        UserId = _currentUserId,
                        Role = GroupRole.Owner
                    });
                    targetGroupId = newGroup.Id;
                }

                foreach (var userId in _selectedUserIds)
                {
                    switch (await AddOrInviteAsync(context, targetGroupId, userId))
                    {
                        case AddResult.Added: added++; break;
                        case AddResult.Invited: invited++; break;
                        default: skipped++; break;
                    }
                }

                await context.SaveChangesAsync();

                var parts = new List<string>();
                if (added > 0) parts.Add($"{added} kiþi gruba eklendi");
                if (invited > 0) parts.Add($"{invited} kiþiye davet gönderildi");
                if (skipped > 0) parts.Add($"{skipped} kiþi atlandý (zaten grupta, davet bekliyor ya da engelli)");

                message = parts.Count > 0 ? string.Join(". ", parts) + "." : "Kimse eklenmedi.";
                if (groupName != null)
                    message = $"\"{groupName}\" grubu oluþturuldu!\n" + message;
            }

            await DisplayAlert("Bilgi", message, "Tamam");
            await Navigation.PopAsync();
        }
    }
}