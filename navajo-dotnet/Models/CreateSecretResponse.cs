using System.Text.Json.Serialization;

namespace navajo_dotnet.Models;

public record CreateSecretResponse
{
    [JsonPropertyName("link")]
    public required string Link { init; get; }
    
    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { init; get; }
}