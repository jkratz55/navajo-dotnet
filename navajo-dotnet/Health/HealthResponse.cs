namespace navajo_dotnet.Health;

public record HealthResponse
{
    public string Status { get; set; }
    public IEnumerable<HealthCheckItem> Checks { get; set; }
    public string Duration { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}