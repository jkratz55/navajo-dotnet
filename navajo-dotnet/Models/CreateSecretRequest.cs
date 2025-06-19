using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace navajo_dotnet.Models;

public record CreateSecretRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(10000)]
    [JsonPropertyName("value")]
    public string Value { get; init; } = default!;
}