namespace navajo_dotnet.Health;

public record HealthCheckItem
{
    public string Name { get; set; }
    public string Status { get; set; }
    public string? Description { get; set; }
    public string Duration { get; set; }
    public IReadOnlyDictionary<string, object> Data { get; set; }
}