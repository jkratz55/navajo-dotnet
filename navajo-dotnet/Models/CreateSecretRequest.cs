using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace navajo_dotnet.Models;

public record CreateSecretRequest(
    [property: Required]
    [property: MinLength(1)]
    [property: MaxLength(10000)]
    [property: JsonPropertyName("value")]
    string Value);
