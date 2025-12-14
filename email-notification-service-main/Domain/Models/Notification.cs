using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

public class Notification
{
    [Key] public Guid Id { get; set; }

    [EnumDataType(typeof(NotificationChannel))]
    public NotificationStatus Status { get; set; }

    [EnumDataType(typeof(NotificationChannel))]
    public NotificationChannel Type { get; set; }

    public string Recipient { get; set; } = null!;
    public int RetryCount { get; set; }
    public int MaxRetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? ErrorMessage { get; set; }
}