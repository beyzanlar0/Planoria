using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Planoria.Models
{
    public class AssignmentItem : INotifyPropertyChanged
    {
        public int AssignmentId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public bool IsMine { get; set; }
        public bool ViewerIsLeader { get; set; }
        public AssignmentStatus Status { get; set; }
        public DateTime? DeadlineAt { get; set; }
        public int TotalMinutes { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string DisplayName => IsMine ? $"{Username} (sen)" : Username;

        private bool IsOverdue =>
            Status == AssignmentStatus.Active && DeadlineAt.HasValue && DateTime.UtcNow >= DeadlineAt.Value;

        public bool CanApprove => ViewerIsLeader && Status == AssignmentStatus.Requested;
        public bool CanComplete => IsMine && Status == AssignmentStatus.Active && !IsOverdue;

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case AssignmentStatus.Requested: return "⏳ Onay bekliyor";
                    case AssignmentStatus.Active:
                        if (IsOverdue) return "⌛ Süre doldu";
                        return "⏱ " + FormatRemaining((DeadlineAt ?? DateTime.UtcNow) - DateTime.UtcNow);
                    case AssignmentStatus.Completed: return "✅ Tamamladı";
                    case AssignmentStatus.Expired: return "⌛ Süre doldu";
                    default: return "";
                }
            }
        }

        // Yeşil → sarı → kırmızı
        public string ColorHex
        {
            get
            {
                switch (Status)
                {
                    case AssignmentStatus.Completed: return "#5BBF94";
                    case AssignmentStatus.Expired: return "#E0607A";
                    case AssignmentStatus.Active:
                        if (IsOverdue || !DeadlineAt.HasValue || TotalMinutes <= 0) return "#E0607A";
                        double ratio = (DeadlineAt.Value - DateTime.UtcNow).TotalMinutes / TotalMinutes;
                        if (ratio > 0.5) return "#5BBF94";
                        if (ratio > 0.2) return "#E8B63F";
                        return "#E0607A";
                    default: return "#C99AAB";
                }
            }
        }

        public void Refresh()
        {
            if (Status != AssignmentStatus.Active) return;
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(ColorHex));
            OnPropertyChanged(nameof(CanComplete));
        }

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private static string FormatRemaining(TimeSpan ts)
        {
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays} gün {ts.Hours} sa kaldı";
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours} sa {ts.Minutes} dk {ts.Seconds} sn kaldı";
            return $"{ts.Minutes} dk {ts.Seconds} sn kaldı";
        }
    }

    public class GroupTaskItem
    {
        public int TaskId { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public int DurationMinutes { get; set; }
        public bool CanRequest { get; set; }
        public bool CanDelete { get; set; }
        public ObservableCollection<AssignmentItem> Assignments { get; } = new();

        public bool HasDescription => !string.IsNullOrWhiteSpace(Description);
        public bool HasAssignments => Assignments.Count > 0;
        public string DurationText => FormatDuration(DurationMinutes);

        public static string FormatDuration(int minutes)
        {
            if (minutes < 60) return $"{minutes} dakika";
            if (minutes < 1440) return $"{minutes / 60} saat";
            return $"{minutes / 1440} gün";
        }
    }
}