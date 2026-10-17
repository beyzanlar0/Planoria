using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class GroupInfoPage : ContentPage
    {
        private readonly int _groupId;
        private readonly int _currentUserId;
        private GroupRole _myRole = GroupRole.Member;
        private readonly ObservableCollection<GroupMemberItem> _members = new();

        public GroupInfoPage(int groupId)
        {
            InitializeComponent();
            _groupId = groupId;
            _currentUserId = Preferences.Get("CurrentUserId", 0);
            MembersCollectionView.ItemsSource = _members;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAll();
        }

        private async Task LoadAll()
        {
            using var context = new AppDbContext();

            var group = await context.Groups.FindAsync(_groupId);
            var members = await context.GroupMembers
                .Where(gm => gm.GroupId == _groupId)
                .Include(gm => gm.User)
                .OrderBy(gm => gm.Role)      // Owner, Admin, Member sýrasý
                .ThenBy(gm => gm.JoinedAt)
                .ToListAsync();

            var me = members.FirstOrDefault(m => m.UserId == _currentUserId);
            if (group == null || me == null)
            {
                // Grup yok ya da gruptan çýkarýlmýþýz
                await Navigation.PopToRootAsync();
                return;
            }

            _myRole = me.Role;
            GroupNameLabel.Text = group.Name;
            MemberCountLabel.Text = $"{members.Count} üye";

            bool canManage = _myRole != GroupRole.Member;
            EditNameButton.IsVisible = canManage;
            AddMemberButton.IsVisible = canManage;

            _members.Clear();
            foreach (var m in members)
            {
                _members.Add(new GroupMemberItem
                {
                    GroupMemberId = m.Id,
                    UserId = m.UserId,
                    Username = m.User?.Username ?? "?",
                    AvatarId = m.User?.AvatarId ?? 0,
                    Role = m.Role,
                    IsMe = m.UserId == _currentUserId
                });
            }
        }

        private async void OnEditNameClicked(object sender, EventArgs e)
        {
            string? newName = await DisplayPromptAsync(
                "Grup Adý", "Yeni grup adýný yaz:", "Kaydet", "Vazgeç",
                initialValue: GroupNameLabel.Text, maxLength: 100);

            if (string.IsNullOrWhiteSpace(newName)) return;

            using var context = new AppDbContext();
            var myRole = await GroupAuth.GetRoleAsync(context, _groupId, _currentUserId);
            if (myRole == null || myRole == GroupRole.Member) { await LoadAll(); return; }

            var group = await context.Groups.FindAsync(_groupId);
            if (group == null) return;

            group.Name = newName.Trim();
            await context.SaveChangesAsync();
            await LoadAll();
        }



        private async void OnAddMemberClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SelectPeoplePage(SelectPeopleMode.AddToGroup, _groupId));
        }

        private async void OnMemberTapped(object sender, EventArgs e)
        {
            var frame = sender as Frame;
            var member = frame?.BindingContext as GroupMemberItem;
            if (member == null || member.IsMe) return;

            // Rolüme göre hangi iþlemleri yapabileceðimi belirle
            var buttons = new List<string> { "Profili Gör" };
            string? destruction = null;

            if (_myRole == GroupRole.Owner)
            {
                if (member.Role == GroupRole.Member) buttons.Add("Yardýmcý Yönetici Yap");
                if (member.Role == GroupRole.Admin) buttons.Add("Üyeliðe Düþür");
                buttons.Add("Yönetici Yap (Sahipliði Devret)");
                destruction = "Gruptan Çýkar";
            }
            else if (_myRole == GroupRole.Admin && member.Role == GroupRole.Member)
            {
                destruction = "Gruptan Çýkar";
            }

            string? choice = await DisplayActionSheet(member.Username, "Vazgeç", destruction, buttons.ToArray());

            switch (choice)
            {
                case "Profili Gör":
                    await Navigation.PushAsync(new MemberProfilePage(member.UserId));
                    break;
                case "Yardýmcý Yönetici Yap":
                    await ChangeRole(member.GroupMemberId, GroupRole.Admin);
                    break;
                case "Üyeliðe Düþür":
                    await ChangeRole(member.GroupMemberId, GroupRole.Member);
                    break;
                case "Yönetici Yap (Sahipliði Devret)":
                    await TransferOwnership(member);
                    break;
                case "Gruptan Çýkar":
                    await RemoveMember(member);
                    break;
            }
        }
        private async Task ChangeRole(int groupMemberId, GroupRole newRole)
        {
            using var context = new AppDbContext();
            if (await GroupAuth.GetRoleAsync(context, _groupId, _currentUserId) != GroupRole.Owner)
            {
                await LoadAll();
                return;
            }

            var gm = await context.GroupMembers
                .FirstOrDefaultAsync(x => x.Id == groupMemberId && x.GroupId == _groupId);
            if (gm == null || gm.UserId == _currentUserId) return;

            gm.Role = newRole;
            await context.SaveChangesAsync();
            await LoadAll();
        }

        private async Task TransferOwnership(GroupMemberItem target)
        {
            bool confirm = await DisplayAlert("Sahipliði Devret",
                $"{target.Username} grubun yöneticisi olacak, sen yardýmcý yönetici olacaksýn. Emin misin?",
                "Evet", "Vazgeç");
            if (!confirm) return;

            using var context = new AppDbContext();
            var myGm = await context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == _groupId && gm.UserId == _currentUserId);
            var targetGm = await context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.Id == target.GroupMemberId && gm.GroupId == _groupId);

            if (myGm == null || myGm.Role != GroupRole.Owner || targetGm == null || targetGm.UserId == _currentUserId)
            {
                await LoadAll();
                return;
            }

            targetGm.Role = GroupRole.Owner;
            myGm.Role = GroupRole.Admin;
            await context.SaveChangesAsync();
            await LoadAll();
        }


        private async Task RemoveMember(GroupMemberItem member)
        {
            bool confirm = await DisplayAlert("Gruptan Çýkar",
                $"{member.Username} gruptan çýkarýlsýn mý?", "Evet", "Vazgeç");
            if (!confirm) return;

            using var context = new AppDbContext();
            var myRole = await GroupAuth.GetRoleAsync(context, _groupId, _currentUserId);
            var gm = await context.GroupMembers
                .FirstOrDefaultAsync(x => x.Id == member.GroupMemberId && x.GroupId == _groupId);

            if (myRole == null || gm == null || gm.UserId == _currentUserId) { await LoadAll(); return; }

            bool allowed = myRole == GroupRole.Owner
                        || (myRole == GroupRole.Admin && gm.Role == GroupRole.Member);
            if (!allowed) { await LoadAll(); return; }

            context.GroupMembers.Remove(gm);
            await context.SaveChangesAsync();
            await LoadAll();
        }
        private async void OnLeaveClicked(object sender, EventArgs e)
        {
            if (_myRole == GroupRole.Owner)
            {
                await DisplayAlert("Ayrýlamazsýn",
                    "Grubun yöneticisisin. Ayrýlmadan önce bir üyeye sahipliði devretmelisin.", "Tamam");
                return;
            }

            bool confirm = await DisplayAlert("Gruptan Ayrýl",
                "Bu gruptan ayrýlmak istediðine emin misin?", "Evet", "Vazgeç");
            if (!confirm) return;

            using var context = new AppDbContext();
            var gm = await context.GroupMembers
                .FirstOrDefaultAsync(x => x.GroupId == _groupId && x.UserId == _currentUserId);
            if (gm != null)
            {
                context.GroupMembers.Remove(gm);
                await context.SaveChangesAsync();
            }

            await Navigation.PopToRootAsync();
        }
    }
}