using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planoria.Api.Dtos;
using Planoria.Data;
using Planoria.Models;

namespace Planoria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;
    public TasksController(AppDbContext db) => _db = db;

    // Kimlik HER ZAMAN token'dan okunur, istekten asla alınmaz
    private int CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id)
            ? id : 0;

    private static DateTime? AsUtc(DateTime? d) =>
     d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : null;

    private static TaskResponse ToResponse(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.RecurrenceType,
        t.DueDate, t.IsCompleted,
        AsUtc(t.CompletedAt),
        DateTime.SpecifyKind(t.CreatedAt, DateTimeKind.Utc));

    // GET api/tasks  -> sadece benim bireysel görevlerim
    [HttpGet]
    public async Task<ActionResult<List<TaskResponse>>> GetMine()
    {
        var userId = CurrentUserId;
        var tasks = await _db.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.GroupId == null)
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        return tasks.Select(ToResponse).ToList();
    }

    // GET api/tasks/5  -> benim değilse 404
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponse>> GetById(int id)
    {
        var userId = CurrentUserId;
        var task = await _db.Tasks
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.GroupId == null);

        return task == null ? NotFound() : ToResponse(task);
    }

    // POST api/tasks
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest req)
    {
        var title = req.Title.Trim();
        if (title.Length == 0)
            return BadRequest(new { error = "Başlık boş olamaz." });

        var task = new TaskItem
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            RecurrenceType = req.RecurrenceType,
            DueDate = req.DueDate,
            UserId = CurrentUserId   // sahibi token'dan
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ToResponse(task));
    }
    // PUT api/tasks/5
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TaskResponse>> Update(int id, UpdateTaskRequest req)
    {
        var title = req.Title.Trim();
        if (title.Length == 0)
            return BadRequest(new { error = "Başlık boş olamaz." });

        var userId = CurrentUserId;
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.GroupId == null);
        if (task == null) return NotFound();

        task.Title = title;
        task.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        task.RecurrenceType = req.RecurrenceType;
        task.DueDate = req.DueDate;

        await _db.SaveChangesAsync();
        return ToResponse(task);
    }

    // DELETE api/tasks/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.GroupId == null);
        if (task == null) return NotFound();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/tasks/5/complete
    [HttpPost("{id:int}/complete")]
    public async Task<ActionResult<TaskResponse>> Complete(int id)
    {
        var userId = CurrentUserId;
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.GroupId == null);
        if (task == null) return NotFound();

        if (!task.IsCompleted)               // zaten tamamsa tarihi değiştirme
        {
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;   // zamanı sunucu verir
            await _db.SaveChangesAsync();
        }
        return ToResponse(task);
    }

    // POST api/tasks/5/reopen
    [HttpPost("{id:int}/reopen")]
    public async Task<ActionResult<TaskResponse>> Reopen(int id)
    {
        var userId = CurrentUserId;
        var task = await _db.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.GroupId == null);
        if (task == null) return NotFound();

        if (task.IsCompleted)
        {
            task.IsCompleted = false;
            task.CompletedAt = null;
            await _db.SaveChangesAsync();
        }
        return ToResponse(task);
    }
}