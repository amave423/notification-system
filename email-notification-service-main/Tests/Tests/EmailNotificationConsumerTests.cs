using Api.Models;
using Domain.Contracts;
using Domain.Models;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Tests;

[TestFixture]
public class EmailNotificationConsumerTests : BaseIntegrationTest
{
    [SetUp]
    public async Task AdditionalSetUp()
    {
        _testHarness = _serviceProvider.GetRequiredService<ITestHarness>();
        _repository = _serviceProvider.GetRequiredService<INotificationRepository>();

        await _testHarness.Start();
    }

    [TearDown]
    public async Task AdditionalTearDown()
    {
        await _testHarness.Stop();
    }

    private ITestHarness _testHarness;
    private INotificationRepository _repository;

    [Test]
    public async Task Consume_WhenEmailSentSuccessfully_ShouldUpdateStatusToSent()
    {
        var message = CreateTestMessage();
        var notification = CreateNotification(message);
        await _repository.InsertAsync(notification);

        await _testHarness.Bus.Publish(message);

        var consumed = await _testHarness.Consumed.Any<NotificationMessage>();
        Assert.That(consumed, Is.True);
        AwaitStatus(notification.Id, NotificationStatus.Sent);

        notification = await _repository.GetByIdAsync(message.NotificationId, true);
        Assert.That(notification, Is.Not.Null);
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(notification.RetryCount, Is.EqualTo(0));
    }

    [Test]
    public async Task Consume_WhenEmailSentFailed_ShouldUpdateStatusToFailed()
    {
        var message = CreateTestMessage();
        var notification = CreateNotification(message);
        await _repository.InsertAsync(notification);
        _emailSenderMock.FailMock(3);

        await _testHarness.Bus.Publish(message);

        var consumed = await _testHarness.Consumed.Any<NotificationMessage>();
        Assert.That(consumed, Is.True);
        AwaitStatus(notification.Id, NotificationStatus.Failed);

        notification = await _repository.GetByIdAsync(message.NotificationId, true);
        Assert.That(notification, Is.Not.Null);
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Failed));
        Assert.That(notification.RetryCount, Is.EqualTo(3));
    }

    [Test]
    public async Task Consume_WhenEmailSentSuccessfullyAfterRetry_ShouldStatusToSent()
    {
        var message = CreateTestMessage();
        var notification = CreateNotification(message);
        await _repository.InsertAsync(notification);
        _emailSenderMock.FailMock(1);

        await _testHarness.Bus.Publish(message);

        var consumed = await _testHarness.Consumed.Any<NotificationMessage>();
        Assert.That(consumed, Is.True);
        AwaitStatus(notification.Id, NotificationStatus.Sent);

        notification = await _repository.GetByIdAsync(message.NotificationId, true);
        Assert.That(notification, Is.Not.Null);
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(notification.RetryCount, Is.EqualTo(1));
    }

    private void AwaitStatus(Guid id, NotificationStatus status)
    {
        Assert.That(() => _repository.GetByIdAsync(id, true).Result!.Status,
            Is.EqualTo(status).After((int)TimeSpan.FromMinutes(1).TotalMilliseconds, 50));
    }
}