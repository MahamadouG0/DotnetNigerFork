namespace DotnetNiger.Community.Domain.Entities;

public class ResourceCategory
{
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
