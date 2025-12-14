using Domain.Contracts;
using EmailNotificationService.Consumers;
using EmailNotificationService.Data;
using EmailNotificationService.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.Elasticsearch;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.Elasticsearch(
        new ElasticsearchSinkOptions(new Uri(builder.Configuration["Elasticsearch:Url"] ?? "http://localhost:9200"))
        {
            AutoRegisterTemplate = true,
            IndexFormat = "email-service-logs-{0:yyyy.MM.dd}"
        })
    .CreateLogger();

builder.Services.AddSerilog();
builder.Logging.ClearProviders();

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmailNotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]);
            h.Password(builder.Configuration["RabbitMQ:Password"]);
        });

        cfg.ReceiveEndpoint("email-queue", e =>
        {
            e.ConfigureConsumer<EmailNotificationConsumer>(context);
            e.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(5)));
            e.Bind("notifications-exchange", x => x.RoutingKey = "email");
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddHttpClient();

builder.Services.AddHostedService<PrometheusExporter>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    context.Database.EnsureCreated();
}

app.UseHttpsRedirection();
/*app.UseAuthorization();*/
/*app.MapControllers();*/


app.Run();