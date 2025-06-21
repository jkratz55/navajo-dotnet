namespace navajo_dotnet.Health;

public record HealthCheckItem(
    string Name,
    string Status,
    string? Description,
    string Duration,
    IReadOnlyDictionary<string, object> Data);
