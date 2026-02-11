// DTO request Identity: UpdateProfileRequest
namespace DotnetNiger.Identity.Application.DTOs.Requests;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
}
