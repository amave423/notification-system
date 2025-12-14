using MassTransit;
using FluentValidation;
using Serilog;
using SmsSenderService.Configuration;
using SmsSenderService.Consumers;
using SmsSenderService.Models;
using SmsSenderService.Validators;
using SmsSenderService.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

builder.Services.Configure<SmsSettings>(
    builder.Configuration.GetSection("SmsSettings"));

builder.Services.AddScoped<ISmsSenderService, SmsSenderService.Services.SmsSenderService>();
builder.Services.AddScoped<IValidator<SmsRequest>, SmsRequestValidator>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SmsNotificationConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("sms.notifications", e =>
        {
            e.Durable = true;
            e.AutoDelete = false;
            e.PrefetchCount = 10;
            e.ConfigureConsumer<SmsNotificationConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "SMS Sender Service is running!");
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        Status = "Healthy",
        Service = "SMS Sender Service",
        Timestamp = DateTime.UtcNow
    });
});

app.MapPost("/api/sms/send-sync", async (
    SmsRequest request,
    IValidator<SmsRequest> validator,
    ISmsSenderService smsSender) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new
        {
            Errors = validationResult.Errors.Select(e => e.ErrorMessage)
        });
    }

    var message = new SmsMessage
    {
        PhoneNumber = request.PhoneNumber,
        Message = request.Message,
        Sender = request.Sender ?? "SERVICE",
        IsFlash = request.IsFlash,
        CallbackData = request.CallbackData
    };

    var result = await smsSender.SendAsync(message);
    return Results.Ok(result);
});

app.MapPost("/api/sms/send-async", async (
    SmsRequest request,
    IValidator<SmsRequest> validator,
    IBus bus) =>
{
    var validationResult = await validator.ValidateAsync(request);
    if (!validationResult.IsValid)
    {
        return Results.BadRequest(new
        {
            Errors = validationResult.Errors.Select(e => e.ErrorMessage)
        });
    }

    var message = new SmsMessage
    {
        PhoneNumber = request.PhoneNumber,
        Message = request.Message,
        Sender = request.Sender ?? "SERVICE",
        IsFlash = request.IsFlash,
        CallbackData = request.CallbackData
    };

    await bus.Publish(message);

    return Results.Accepted($"/api/sms/status/{message.Id}", new
    {
        MessageId = message.Id,
        Status = "Queued",
        QueuedAt = DateTime.UtcNow,
        Message = "SMS has been queued for processing"
    });
});

app.MapGet("/api/sms/status/{id:guid}", (Guid id) =>
{
    return Results.Ok(new
    {
        MessageId = id,
        Status = "Processing",
        LastUpdated = DateTime.UtcNow.AddMinutes(-1),
        Note = "This is a mock response. In production, query from database."
    });
});

Log.Information("SMS Sender Service starting...");
app.Run();