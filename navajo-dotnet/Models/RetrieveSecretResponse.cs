using System.Text.Json.Serialization;

namespace navajo_dotnet.Models;

public record RetrieveSecretResponse
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}