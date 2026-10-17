using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Dispatching;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class GroupTasksPage : ContentPage
    {
        private readonly int _groupId;
        private readonly int _currentUserId;
        private bool _isLeader;   // Yönetici veya yardýmcý yönetici
        private bool _isOwner;    // Sadece Yönetici
        private readonly ObservableCollection<GroupTaskItem> _tasks = new();
        private IDispatcherTimer? _timer;

        private static readonly (string Label, int Minutes)[] DurationOptions =
        {
            ("30 dakika", 30), ("1 saat", 60), ("2 saat", 120), ("6 saat", 360),
            ("12 saat", 720), ("1 gün", 1440), ("3 gün", 4320), ("1 hafta", 10080)
        };

        public GroupTasksPage(int groupId)
        {
            InitializeComponent();
            _groupId = groupId;
            _currentUserId = Preferences.Get("CurrentUserId", 0);

            TasksCollectionView.ItemsSource = _tasks;
            DurationPicker.ItemsSource = DurationOptions.Select(o => o.Label).ToList();
            DurationPicker.SelectedIndex = 2;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAll();

            if (_timer == null)
            {
                _timer = Dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += (s, e) =>
                {
                    foreach (var task in _tasks)
                        foreach (var a in task.Assignments)
                            a.Refresh();
                };
            }
            _timer.Start();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _timer?.Stop();
        }

        private async Task LoadAll()
        {
            using var context = new AppDbContext();

            var group = await context.Groups.FindAsync(_groupId);
            var me = await context.GroupMembers
                .FirstOrDefaultAsync(gm => gm.GroupId == _groupId && gm.UserId == _currentUserId);

            if (group == null || me == null)
            {
                await Navigation.PopToRootAsync();
                return;
            }

            GroupNameLabel.Text = group.Name;
            _isLeader = me.Role != GroupRole.Member;
            _isOwner = me.Role == GroupRole.Owner;
            LeaderPanel.IsVisible = _isLeader;

            // Süresi dolan aktif görevleri kalýcý olarak "Expired" yap
            var now = DateTime.UtcNow;
            var overdue = await context.GroupTaskAssignments
                .Where(a => a.GroupTask!.GroupId == _groupId
                            && a.Status == AssignmentStatus.Active
                            && a.DeadlineAt != null && a.DeadlineAt < now)
                .ToListAsync();
            foreach (var a in overdue) a.Status = AssignmentStatus.Expired;
            if (overdue.Any()) await context.SaveChangesAsync();

            var tasks = await context.GroupTasks
                .Where(t => t.GroupId == _groupId)
                .Include(t => t.Assignments).ThenInclude(a => a.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            _tasks.Clear();
            foreach (var t in tasks)
            {
                var mine = t.Assignments.FirstOrDefault(a => a.UserId == _currentUserId);

                // Görevi þu an biri üstlenmiþ mi (aktif) ya da bitirmiþ mi?
                bool taken = t.Assignments.Any(a =>
                    a.Status == AssignmentStatus.Active || a.Status == AssignmentStatus.Completed);

                // Daha önce talep etmediysem ya da talebim reddedildi / süresi doldu ise tekrar deneyebilirim
                bool canRetry = mine == null
                    || mine.Status == AssignmentStatus.Rejected
                    || mine.Status == AssignmentStatus.Expired;

                var item = new GroupTaskItem
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    Description = t.Description ?? "",
                    DurationMinutes = t.DurationMinutes,
                    CanDelete = _isLeader,
                    CanRequest = !taken && canRetry
                };

                foreach (var a in t.Assignments
                             .Where(a => a.Status != AssignmentStatus.Rejected)
                             .OrderBy(a => a.RequestedAt))
                {
                    item.Assignments.Add(new AssignmentItem
                    {
                        AssignmentId = a.Id,
                        UserId = a.UserId,
                        Username = a.User?.Username ?? "?",
                        IsMine = a.UserId == _currentUserId,
                        ViewerIsLeader = _isOwner,   // onay/ret butonlarý sadece Yönetici'de görünür
                        Status = a.Status,
                        DeadlineAt = a.DeadlineAt,
                        TotalMinutes = t.DurationMinutes
                    });
                }

                _tasks.Add(item);
            }
        }

        private void OnToggleFormClicked(object sender, EventArgs e)
        {
            NewTaskForm.IsVisible = !NewTaskForm.IsVisible;
        }

        private async void OnCreateTaskClicked(object sender, EventArgs e)
        {
            if (!_isLeader) return;

            string title = TitleEntry.Text;
            if (string.IsNullOrWhiteSpace(title))
            {
                await DisplayAlert("Uyarý", "Görev baþlýðý boþ olamaz.", "Tamam");
                return;
            }

            int idx = DurationPicker.SelectedIndex < 0 ? 2 : DurationPicker.SelectedIndex;

            using (var context = new AppDbContext())
            {
                context.GroupTasks.Add(new GroupTask
                {
                    GroupId = _groupId,
                    Title = title.Trim(),
                    Description = string.IsNullOrWhiteSpace(DescriptionEditor.Text) ? null : DescriptionEditor.Text.Trim(),
                    DurationMinutes = DurationOptions[idx].Minutes,
                    CreatedByUserId = _currentUserId
                });
                await context.SaveChangesAsync();
            }

            TitleEntry.Text = string.Empty;
            DescriptionEditor.Text = string.Empty;
            NewTaskForm.IsVisible = false;
            await LoadAll();
        }

        private async void OnRequestClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as GroupTaskItem;
            if (item == null) return;

            bool alreadyTaken = false;

            using (var context = new AppDbContext())
            {
                // Butona basana kadar baþkasý almýþ olabilir, tekrar kontrol et
                alreadyTaken = await context.GroupTaskAssignments.AnyAsync(a =>
                    a.GroupTaskId == item.TaskId &&
                    (a.Status == AssignmentStatus.Active || a.Status == AssignmentStatus.Completed));

                if (!alreadyTaken)
                {
                    var existing = await context.GroupTaskAssignments
                        .FirstOrDefaultAsync(a => a.GroupTaskId == item.TaskId && a.UserId == _currentUserId);

                    if (existing == null)
                    {
                        context.GroupTaskAssignments.Add(new GroupTaskAssignment
                        {
                            GroupTaskId = item.TaskId,
                            UserId = _currentUserId
                        });
                    }
                    else if (existing.Status == AssignmentStatus.Rejected || existing.Status == AssignmentStatus.Expired)
                    {
                        existing.Status = AssignmentStatus.Requested;
                        existing.RequestedAt = DateTime.UtcNow;
                        existing.StartedAt = null;
                        existing.DeadlineAt = null;
                    }
                    await context.SaveChangesAsync();
                }
            }

            if (alreadyTaken)
                await DisplayAlert("Bilgi", "Bu görev az önce baþkasý tarafýndan alýndý.", "Tamam");

            await LoadAll();
        }

        private async void OnApproveClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as AssignmentItem;
            if (item == null || !_isOwner) return;

            bool alreadyTaken = false;

            using (var context = new AppDbContext())
            {
                var a = await context.GroupTaskAssignments
                    .Include(x => x.GroupTask)
                    .FirstOrDefaultAsync(x => x.Id == item.AssignmentId);

                if (a != null && a.Status == AssignmentStatus.Requested)
                {
                    // Görev bu arada baþkasýna verilmiþ mi?
                    alreadyTaken = await context.GroupTaskAssignments.AnyAsync(x =>
                        x.GroupTaskId == a.GroupTaskId && x.Id != a.Id &&
                        (x.Status == AssignmentStatus.Active || x.Status == AssignmentStatus.Completed));

                    if (!alreadyTaken)
                    {
                        var now = DateTime.UtcNow;
                        a.Status = AssignmentStatus.Active;
                        a.StartedAt = now;
                        a.DeadlineAt = now.AddMinutes(a.GroupTask!.DurationMinutes);

                        // Ayný göreve gelen diðer bekleyen talepleri kapat
                        var others = await context.GroupTaskAssignments
                            .Where(x => x.GroupTaskId == a.GroupTaskId
                                        && x.Id != a.Id
                                        && x.Status == AssignmentStatus.Requested)
                            .ToListAsync();
                        foreach (var o in others)
                            o.Status = AssignmentStatus.Rejected;

                        await context.SaveChangesAsync();
                    }
                }
            }

            if (alreadyTaken)
                await DisplayAlert("Bilgi", "Bu görev zaten baþka bir üyeye verilmiþ.", "Tamam");

            await LoadAll();
        }

        private async void OnRejectClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as AssignmentItem;
            if (item == null || !_isOwner) return;

            using (var context = new AppDbContext())
            {
                var a = await context.GroupTaskAssignments.FindAsync(item.AssignmentId);
                if (a != null && a.Status == AssignmentStatus.Requested)
                {
                    a.Status = AssignmentStatus.Rejected;
                    await context.SaveChangesAsync();
                }
            }

            await LoadAll();
        }

        private async void OnCompleteClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as AssignmentItem;
            if (item == null) return;

            using (var context = new AppDbContext())
            {
                var a = await context.GroupTaskAssignments.FindAsync(item.AssignmentId);
                if (a != null
                    && a.UserId == _currentUserId
                    && a.Status == AssignmentStatus.Active
                    && a.DeadlineAt > DateTime.UtcNow)
                {
                    a.Status = AssignmentStatus.Completed;
                    a.CompletedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }

            await LoadAll();
        }

        private async void OnDeleteTaskClicked(object sender, EventArgs e)
        {
            var item = (sender as Button)?.BindingContext as GroupTaskItem;
            if (item == null || !_isLeader) return;

            bool confirm = await DisplayAlert("Görevi Sil",
                $"\"{item.Title}\" görevi ve tüm talepleri silinsin mi?", "Evet", "Vazgeç");
            if (!confirm) return;

            using (var context = new AppDbContext())
            {
                var task = await context.GroupTasks.FindAsync(item.TaskId);
                if (task != null)
                {
                    context.GroupTasks.Remove(task);
                    await context.SaveChangesAsync();
                }
            }

            await LoadAll();
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadAll();
            TasksRefreshView.IsRefreshing = false;
        }
    }
}