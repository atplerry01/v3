namespace Whycespace.FoundationHost.Workers;

using global::System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Whycespace.Runtime.Events;
using Whycespace.Shared.Events;

public sealed class KafkaConsumerWorker : BackgroundService
{
    private readonly EventBus _eventBus;
    private readonly ILogger<KafkaConsumerWorker> _logger;
    private readonly string _bootstrapServers;
    private IConsumer<string, string>? _consumer;

    public KafkaConsumerWorker(
        EventBus eventBus,
        IConfiguration configuration,
        ILogger<KafkaConsumerWorker> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KafkaConsumerWorker starting — connecting to {Brokers}", _bootstrapServers);

        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = "foundation-host",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _consumer.Subscribe(new[] { "whyce.events", "whyce.commands", "whyce.workflows" });

            _logger.LogInformation("KafkaConsumerWorker connected — consuming topics");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(TimeSpan.FromMilliseconds(500));
                    if (result is null) continue;

                    var payload = JsonSerializer.Deserialize<Dictionary<string, object>>(result.Message.Value)
                        ?? new Dictionary<string, object>();

                    var eventType = payload.GetValueOrDefault("EventType") as string ?? "Unknown";
                    var aggregateIdStr = payload.GetValueOrDefault("AggregateId") as string;
                    var aggregateId = Guid.TryParse(aggregateIdStr, out var parsed) ? parsed : Guid.NewGuid();

                    var systemEvent = SystemEvent.Create(eventType, aggregateId, payload);
                    await _eventBus.PublishAsync(systemEvent);

                    _logger.LogDebug("Consumed Kafka message: {EventType} from {Topic}",
                        eventType, result.Topic);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogWarning(ex, "Kafka consume error");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka consumer unavailable — running without Kafka consumption");
        }
        finally
        {
            _consumer?.Close();
            _consumer?.Dispose();
        }

        _logger.LogInformation("KafkaConsumerWorker stopped");
    }
}
