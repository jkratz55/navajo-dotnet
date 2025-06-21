using System.Text.Json.Serialization;

namespace navajo_dotnet.Models;

public record CreateSecretResponse(
    [property: JsonPropertyName("link")] 
    string Link,

    [property: JsonPropertyName("expiresAt")]
    DateTimeOffset ExpiresAt
);