using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class CreatePartnerRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string PartnerType { get; set; } = "sponsor";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdatePartnerRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string PartnerType { get; set; } = "sponsor";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PartnerResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public string PartnerType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
