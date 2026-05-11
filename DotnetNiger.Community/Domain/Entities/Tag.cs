namespace DotnetNiger.Community.Domain.Entities;

public class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int PostCount { get; set; }

    public ICollection<PostTag> PostTags { get; set; } = [];
}
