using System.Text.Json;
using System.Threading.Channels;
using Confluent.Kafka;
using HandlingLongRunningTask.Kafka.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddHostedService<JobProcessingWorker>();

var host = builder.Build();
await host.RunAsync();

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string GroupId { get; set; } = "long-running-job-workers";
    public string Topic { get; set; } = "long-running-jobs";
}

public class JobProcessingWorker : BackgroundService
{
    private readonly KafkaSettings _kafkaSettings;
    private readonly ILogger<JobProcessingWorker> _logger;
    private IConsumer<Ignore, string>? _consumer;

    // Channel carries both the job and the original ConsumeResult for safe offset commit
    private Channel<(JobPayload Job, ConsumeResult<Ignore, string> ConsumeResult)>? _jobChannel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public JobProcessingWorker(IOptions<KafkaSettings> kafkaSettings, ILogger<JobProcessingWorker> logger)
    {
        _kafkaSettings = kafkaSettings.Value;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Processing Worker starting...");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaSettings.BootstrapServers,
            GroupId = _kafkaSettings.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            MaxPollIntervalMs = 2 * 3600 * 1000, // 2 hours — safe for jobs up to 1 hour
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 10000
        };

        _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Reason}", e.Reason))
            .SetValueDeserializer(Deserializers.Utf8)
            .Build();

        _consumer.Subscribe(_kafkaSettings.Topic);

        // Bounded channel with backpressure
        _jobChannel = Channel.CreateBounded<(JobPayload, ConsumeResult<Ignore, string>)>(
            new BoundedChannelOptions(50)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = false
            });

        // Start multiple background processors for parallelism
        var processorCount = Environment.ProcessorCount * 2;
        for (int i = 0; i < processorCount; i++)
        {
            Task.Run(() => ProcessJobsAsync(cancellationToken), cancellationToken);
        }

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer!.Consume(stoppingToken);

                    if (consumeResult?.Message?.Value != null)
                    {
                        var job = JsonSerializer.Deserialize<JobPayload>(consumeResult.Message.Value, JsonOptions);

                        if (job != null)
                        {
                            _logger.LogInformation("Received job: {JobId} - {Description}", job.JobId, job.Description);

                            // Pass both the job and the original consume result
                            await _jobChannel!.Writer.WriteAsync((job, consumeResult), stoppingToken);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize message: {Message}", consumeResult.Message.Value);
                            // Commit invalid messages to avoid infinite retry loop
                            _consumer.Commit(consumeResult);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Reason}", ex.Error.Reason);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            _consumer?.Close();
        }
    }

    private async Task ProcessJobsAsync(CancellationToken ct)
    {
        await foreach (var (job, consumeResult) in _jobChannel!.Reader.ReadAllAsync(ct))
        {
            _logger.LogInformation("Started processing job {JobId}: {Description}", job.JobId, job.Description);

            try
            {
                // Simulate long-running work (5 seconds = 1 simulated hour)
                for (int hour = 1; hour <= job.SimulatedHours; hour++)
                {
                    if (ct.IsCancellationRequested) break;
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                    _logger.LogInformation("Job {JobId} progress: {Hour}/{Total} hours", job.JobId, hour, job.SimulatedHours);
                }

                // Commit the exact offset of this specific message
                _consumer!.Commit(consumeResult);

                _logger.LogInformation("Completed job {JobId}", job.JobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed job {JobId}", job.JobId);
                // Do NOT commit on failure → will be retried on next startup
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job Processing Worker stopping...");
        _jobChannel?.Writer.Complete(); // Signal no more jobs will be added
        return base.StopAsync(cancellationToken);
    }
}