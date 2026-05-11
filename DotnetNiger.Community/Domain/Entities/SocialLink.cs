namespace DotnetNiger.Community.Domain.Entities;

public class SocialLink
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public Member Member { get; set; } = null!;
}
