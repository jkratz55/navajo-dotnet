namespace navajo_dotnet.Health;

public record HealthResponse(
    string Status,
    IEnumerable<HealthCheckItem> Checks,
    string Duration,
    DateTimeOffset Timestamp);
