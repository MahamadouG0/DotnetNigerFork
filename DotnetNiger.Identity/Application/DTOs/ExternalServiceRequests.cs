using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs;

public record RegisterExternalServiceRequest(
    [Required][StringLength(200, MinimumLength = 1)] string Name,
    [Required][RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens")] string Slug,
    [Required][Url] string BaseUrl,
    string? Description,
    string? HealthEndpoint);

public record UpdateExternalServiceRequest(
    [Url] string? BaseUrl,
    string? Description,
    string? HealthEndpoint);
