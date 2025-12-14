using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SmsSenderService.Configuration;

namespace SmsSenderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueueController : ControllerBase
{
    private readonly ILogger<QueueController> _logger;
    private readonly IConnectionFactory _connectionFactory;
    private readonly RabbitMqSettings _settings;

    public QueueController(
        ILogger<QueueController> logger,
        IConnectionFactory connectionFactory,
        IOptions<RabbitMqSettings> settings)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _settings = settings.Value;
    }

    [HttpPost("create")]
    public IActionResult CreateQueues()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(
                queue: _settings.SmsQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            _logger.LogInformation("Queue {QueueName} created successfully", _settings.SmsQueue);

            return Ok(new
            {
                Message = $"Queue '{_settings.SmsQueue}' created successfully",
                QueueName = _settings.SmsQueue
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create queue {QueueName}", _settings.SmsQueue);
            return StatusCode(500, new
            {
                Error = $"Failed to create queue: {ex.Message}"
            });
        }
    }

    [HttpGet("list")]
    public IActionResult ListQueues()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            var queues = channel.QueueDeclarePassive(_settings.SmsQueue);

            return Ok(new
            {
                QueueName = _settings.SmsQueue,
                MessageCount = queues.MessageCount,
                ConsumerCount = queues.ConsumerCount
            });
        }
        catch (Exception)
        {
            return Ok(new
            {
                QueueName = _settings.SmsQueue,
                Message = "Queue does not exist"
            });
        }
    }
}