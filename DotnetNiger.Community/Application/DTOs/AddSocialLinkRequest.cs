using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class AddSocialLinkRequest
{
    [Required]
    public string Platform { get; set; } = string.Empty;

    [Required, Url]
    public string Url { get; set; } = string.Empty;
}
