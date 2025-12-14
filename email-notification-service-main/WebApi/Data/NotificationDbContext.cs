using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace EmailNotificationService.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.Type).HasConversion<int>();
            entity.Property(e => e.Recipient).HasMaxLength(255);
            entity.Property(e => e.RetryCount);
            entity.Property(e => e.MaxRetryCount);
            entity.Property(e => e.CreatedAt);
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
        });
    }
}