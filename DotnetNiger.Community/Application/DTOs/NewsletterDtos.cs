namespace DotnetNiger.Community.Application.DTOs;

public record SubscribeRequest(string Email, string Name);

public record UnsubscribeRequest(string Email, string Token);

public record NewsletterSubscriptionResponse(
    Guid Id, string Email, string Name, bool IsActive, DateTime SubscribedAt);

public record SendNewsletterRequest(string Subject, string BodyHtml);
