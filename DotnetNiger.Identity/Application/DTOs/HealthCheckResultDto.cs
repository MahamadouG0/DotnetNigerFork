using System.Text.Json.Serialization;

namespace DotnetNiger.Identity.Application.DTOs;

public record HealthCheckResultDto
{
    [JsonPropertyName("isHealthy")]
    public bool IsHealthy { get; init; }
}
