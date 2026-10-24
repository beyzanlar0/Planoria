using System.ComponentModel.DataAnnotations;
using Planoria.Models;

namespace Planoria.Api.Dtos;

// İstemcinin göndereceği alanlar (UserId, CreatedAt yok: sunucu belirler)
public class CreateTaskRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(1000)]
    public string? Description { get; set; }

    public RecurrenceType RecurrenceType { get; set; }

    public DateTime DueDate { get; set; }
}

// İstemciye döneceğimiz alanlar
public record TaskResponse(
    int Id,
    string Title,
    string? Description,
    RecurrenceType RecurrenceType,
    DateTime DueDate,
    bool IsCompleted,
    DateTime? CompletedAt,
    DateTime CreatedAt);