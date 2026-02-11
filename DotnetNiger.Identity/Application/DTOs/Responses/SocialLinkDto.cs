// DTO response Identity: SocialLinkDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

public class SocialLinkDto
{
    public Guid Id { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
