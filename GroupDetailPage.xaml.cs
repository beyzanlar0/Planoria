using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class GroupDetailPage : ContentPage
    {
        private readonly AppDbContext _context;
        private readonly int _groupId;
        private ObservableCollection<TaskItem> _tasks;

        public GroupDetailPage(int groupId)
        {
            InitializeComponent();
            _context = new AppDbContext();
            _groupId = groupId;
            _tasks = new ObservableCollection<TaskItem>();
            TasksCollectionView.ItemsSource = _tasks;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            var group = await _context.Groups.FindAsync(_groupId);
            if (group != null)
                GroupNameLabel.Text = group.Name;

            await LoadTasks();
        }

        private async Task LoadTasks()
        {
            var tasks = await _context.Tasks
                .Where(t => t.GroupId == _groupId)
                .Include(t => t.User)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            _tasks.Clear();
            foreach (var task in tasks)
                _tasks.Add(task);
        }

        private async void OnAddTaskClicked(object sender, EventArgs e)
        {
            string title = NewTaskEntry.Text;
            if (string.IsNullOrWhiteSpace(title)) return;

            int userId = Preferences.Get("CurrentUserId", 0);

            var newTask = new TaskItem
            {
                Title = title,
                UserId = userId,
                GroupId = _groupId,
                RecurrenceType = RecurrenceType.Daily,
                DueDate = DateTime.Now,
                IsCompleted = false
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            await LoadTasks();
            NewTaskEntry.Text = string.Empty;
        }

        private async void OnTaskCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var task = checkbox?.BindingContext as TaskItem;
            if (task == null) return;

            task.IsCompleted = e.Value;
            task.CompletedAt = e.Value ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();
        }

        private async void OnDeleteTaskClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var task = button?.BindingContext as TaskItem;
            if (task == null) return;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            _tasks.Remove(task);
        }
    }
}