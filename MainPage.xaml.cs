using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using Planoria.Data;
using Planoria.Models;

namespace Planoria
{
    public partial class MainPage : ContentPage
    {
        private readonly AppDbContext _context;
        private ObservableCollection<TaskItem> _tasks;
        private RecurrenceType _currentFilter = RecurrenceType.Daily;

        public MainPage()
        {
            InitializeComponent();
            _context = new AppDbContext();
            _tasks = new ObservableCollection<TaskItem>();
            TasksCollectionView.ItemsSource = _tasks;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadTasks();
        }

        private async Task LoadTasks()
        {
            int userId = Preferences.Get("CurrentUserId", 0);

            var tasks = await _context.Tasks
                .Where(t => t.UserId == userId && t.RecurrenceType == _currentFilter)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            _tasks.Clear();
            foreach (var task in tasks)
                _tasks.Add(task);
        }

        private async void OnAddTaskClicked(object sender, EventArgs e)
        {
            string title = NewTaskEntry.Text;

            if (string.IsNullOrWhiteSpace(title))
                return;

            int userId = Preferences.Get("CurrentUserId", 0);

            var newTask = new TaskItem
            {
                Title = title,
                UserId = userId,
                RecurrenceType = _currentFilter,
                DueDate = DateTime.Now,
                IsCompleted = false
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            _tasks.Add(newTask);
            NewTaskEntry.Text = string.Empty;
        }

        private async void OnFilterClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            string parameter = button?.CommandParameter as string;

            if (string.IsNullOrEmpty(parameter))
                return;

            _currentFilter = Enum.Parse<RecurrenceType>(parameter);

            // Sekme renklerini güncelle
            ResetTabColors();
            button.BackgroundColor = Color.FromArgb("#F4A6C1");
            button.TextColor = Colors.White;

            await LoadTasks();
        }

        private void ResetTabColors()
        {
            DailyTabButton.BackgroundColor = Colors.White;
            DailyTabButton.TextColor = Color.FromArgb("#D98BA8");

            WeeklyTabButton.BackgroundColor = Colors.White;
            WeeklyTabButton.TextColor = Color.FromArgb("#D98BA8");

            MonthlyTabButton.BackgroundColor = Colors.White;
            MonthlyTabButton.TextColor = Color.FromArgb("#D98BA8");

            YearlyTabButton.BackgroundColor = Colors.White;
            YearlyTabButton.TextColor = Color.FromArgb("#D98BA8");
        }

        private async void OnTaskCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            var checkbox = sender as CheckBox;
            var task = checkbox?.BindingContext as TaskItem;

            if (task == null || e.Value == (task.CompletedAt != null)) return;
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

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadTasks();
            TasksRefreshView.IsRefreshing = false;
        }
    }
}