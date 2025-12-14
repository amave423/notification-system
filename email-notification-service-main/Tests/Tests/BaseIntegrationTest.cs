using Api.Models;
using Domain.Contracts;
using Domain.Models;
using EmailNotificationService.Consumers;
using EmailNotificationService.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Tests.Tests;

[TestFixture]
public abstract class BaseIntegrationTest
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
       
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:15")
            .WithDatabase("test_db")
            .WithUsername("postgres")
            .WithPassword("password")
            .Build();

        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("guest")
            .WithPassword("guest")
            .Build();

        await _postgresContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

       
        var services = new ServiceCollection();

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(_postgresContainer.GetConnectionString()));

        services.AddMassTransitTestHarness(cfg => { cfg.AddConsumer<EmailNotificationConsumer>(); });

        _emailSenderMock = new MockEmailSender();
        services.AddScoped<IEmailSender>(_ => _emailSenderMock);
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddHttpClient();

       
        services.AddLogging(builder => builder.AddConsole());

        _serviceProvider = services.BuildServiceProvider();

       
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _serviceProvider.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        await _rabbitMqContainer.DisposeAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        context.Notifications.RemoveRange(context.Notifications);
        await context.SaveChangesAsync();
    }

    protected PostgreSqlContainer _postgresContainer;
    protected RabbitMqContainer _rabbitMqContainer;
    protected ServiceProvider _serviceProvider;
    protected MockEmailSender _emailSenderMock;

    protected static NotificationMessage CreateTestMessage()
    {
        return new NotificationMessage
        {
            NotificationId = Guid.NewGuid(),
            Recipient = "test@example.com",
            Subject = "Test Subject",
            Message = "<p>Test Body</p>",
            RetryCount = 0,
            MaxRetryCount = 3
        };
    }

    protected static Notification CreateNotification(NotificationMessage message)
    {
        return new Notification
        {
            Id = message.NotificationId,
            Status = NotificationStatus.Pending,
            Type = NotificationChannel.Email,
            Recipient = message.Recipient,
            RetryCount = message.RetryCount,
            MaxRetryCount = message.MaxRetryCount,
            CreatedAt = DateTime.UtcNow
        };
    }
}