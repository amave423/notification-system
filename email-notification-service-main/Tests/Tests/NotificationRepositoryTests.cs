using Domain.Contracts;
using Domain.Models;
using EmailNotificationService.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Tests;

[TestFixture]
public class NotificationRepositoryTests : BaseIntegrationTest
{
    [OneTimeSetUp]
    public void AdditionalSetUp()
    {
        _context = _serviceProvider.GetRequiredService<NotificationDbContext>();
        _repository = _serviceProvider.GetRequiredService<INotificationRepository>();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private INotificationRepository _repository;
    private NotificationDbContext _context;

    [Test]
    public async Task GetByIdAsync_WhenNotificationExists_ShouldReturnNotification()
    {
       
        var notificationId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = notificationId,
            Recipient = "test@example.com",
            Status = NotificationStatus.Pending
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

       
        var result = await _repository.GetByIdAsync(notificationId);

       
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(notificationId));
        Assert.That(result.Recipient, Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task GetByIdAsync_WhenNotificationDoesNotExist_ShouldReturnNull()
    {
       
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

       
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateStatusAsync_ShouldUpdateNotificationCorrectly()
    {
       
        var notificationId = Guid.NewGuid();
        var notification = new Notification
        {
            Id = notificationId,
            Recipient = "test@example.com",
            Status = NotificationStatus.Pending,
            RetryCount = 0,
            MaxRetryCount = 3
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

       
        await _repository.UpdateStatusAsync(
            notificationId,
            NotificationStatus.Sent,
            1,
            "Test error");

       
        var updatedNotification = await _context.Notifications.FindAsync(notificationId);
        Assert.That(updatedNotification, Is.Not.Null);
        Assert.That(updatedNotification.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(updatedNotification.RetryCount, Is.EqualTo(1));
        Assert.That(updatedNotification.ErrorMessage, Is.EqualTo("Test error"));
        Assert.That(updatedNotification.UpdatedAt, Is.Not.Null);
    }

    [Test]
    public async Task UpdateStatusAsync_WhenNotificationNotFound_ShouldNotThrow()
    {
       
        Assert.DoesNotThrowAsync(async () =>
            await _repository.UpdateStatusAsync(Guid.NewGuid(), NotificationStatus.Sent));
    }
}