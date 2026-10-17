using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planoria.Data;
using Planoria.Models;
using Microsoft.EntityFrameworkCore;

namespace Planoria.Services
{

    public class TaskService
    {
        private readonly AppDbContext _context;

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        // Belirli bir kullanıcının tüm görevlerini getir
        public async Task<List<TaskItem>> GetTasksByUserAsync(int userId)
        {
            return await _context.Tasks
                .Where(t => t.UserId == userId)
                .OrderBy(t => t.DueDate)
                .ToListAsync();
        }

        // Yeni görev ekle
        public async Task<TaskItem> AddTaskAsync(TaskItem task)
        {
            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        // Görevi tamamlandı/tamamlanmadı olarak işaretle
        public async Task ToggleCompleteAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
                task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
                await _context.SaveChangesAsync();
            }
        }

        // Görevi sil
        public async Task DeleteTaskAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
            }
        }
    }
}
