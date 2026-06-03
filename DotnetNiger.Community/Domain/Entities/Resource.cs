namespace DotnetNiger.Community.Domain.Entities;

public class Resource
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public int ViewCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ResourceCategory> ResourceCategories { get; set; } = [];
    public ICollection<ResourceTag> ResourceTags { get; set; } = [];
}
