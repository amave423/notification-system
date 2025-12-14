using Domain.Models;

namespace Domain.Contracts;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, bool asNoTracking = false);
    Task UpdateStatusAsync(Guid id, NotificationStatus status, int retryCount = 0, string? error = null);
    Task<Notification?> InsertAsync(Notification notification);
}