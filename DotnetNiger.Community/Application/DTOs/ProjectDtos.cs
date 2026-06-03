using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class CreateProjectRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
    public string GithubUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Technologies { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }
}

public class UpdateProjectRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
    public string GithubUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Technologies { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }
}

public class ProjectResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string GithubUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Technologies { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
