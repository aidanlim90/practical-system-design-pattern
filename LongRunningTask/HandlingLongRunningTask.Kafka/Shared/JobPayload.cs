namespace HandlingLongRunningTask.Kafka.Shared;

public record JobPayload(
    string JobId,
    string Description,
    int SimulatedHours = 1  // For testing: how many hours to simulate
);