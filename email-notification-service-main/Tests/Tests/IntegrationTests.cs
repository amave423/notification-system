using Api.Models;
using Domain.Contracts;
using Domain.Models;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Tests;

[TestFixture]
public class IntegrationTests : BaseIntegrationTest
{
    [Test]
    public async Task FullFlow_WhenMessageProcessed_ShouldCompleteSuccessfully()
    {
        var testHarness = _serviceProvider.GetRequiredService<ITestHarness>();
        await testHarness.Start();
        var repository = _serviceProvider.GetRequiredService<INotificationRepository>();
        var message = CreateTestMessage();
        var notification = CreateNotification(message);
        await repository.InsertAsync(notification);

        await testHarness.Bus.Publish(message);
        await Task.Delay(1000);

        var consumed = await testHarness.Consumed.Any<NotificationMessage>();
        Assert.That(consumed, Is.True);

        notification = await repository.GetByIdAsync(message.NotificationId, true);
        Assert.That(notification, Is.Not.Null);
        Assert.That(notification.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(notification.Recipient, Is.EqualTo(message.Recipient));

        await testHarness.Stop();
    }
}