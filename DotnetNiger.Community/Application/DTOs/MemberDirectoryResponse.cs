namespace DotnetNiger.Community.Application.DTOs;

public class MemberDirectoryResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<SocialLinkResponse> SocialLinks { get; set; } = [];
}
