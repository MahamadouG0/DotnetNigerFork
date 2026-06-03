namespace DotnetNiger.Community.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string GithubUrl { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Technologies { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public Guid CreatedBy { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
