namespace DotnetNiger.Community.Domain.Entities;

public class ResourceTag
{
    public Guid ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
