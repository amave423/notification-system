using Domain.Contracts;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailNotificationService.Data;

public class NotificationRepository(NotificationDbContext context) : INotificationRepository
{
    public async Task<Notification?> GetByIdAsync(Guid id, bool asNoTracking = false)
    {
        if (asNoTracking)
            return context.Notifications.AsNoTracking().FirstOrDefault(x => x.Id == id);

        return await context.Notifications.FindAsync(id);
    }

    public async Task UpdateStatusAsync(Guid id, NotificationStatus status, int retryCount = 0, string? error = null)
    {
        var notification = await context.Notifications.FindAsync(id);
        if (notification != null)
        {
            notification.Status = status;
            notification.RetryCount = retryCount;
            notification.ErrorMessage = error;
            notification.UpdatedAt = DateTime.UtcNow;
            context.Notifications.Update(notification);
            await context.SaveChangesAsync();
        }
    }

    public async Task<Notification?> InsertAsync(Notification notification)
    {
        var res = await context.Notifications.AddAsync(notification);
        await context.SaveChangesAsync();

        return res.Entity;
    }
}