// DTO request Identity: AddSocialLinkRequest
using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Identity.Application.DTOs.Requests;

public class AddSocialLinkRequest
{
    [Required]
    public string Platform { get; set; } = string.Empty; // Twitter, LinkedIn, GitHub, Facebook

    [Required]
    [Url]
    public string Url { get; set; } = string.Empty;
}
