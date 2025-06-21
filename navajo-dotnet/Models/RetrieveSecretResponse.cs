using System.Text.Json.Serialization;

namespace navajo_dotnet.Models;

public record RetrieveSecretResponse(
    [property: JsonPropertyName("value")]
    string Value
);