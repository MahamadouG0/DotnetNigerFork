using DotnetNiger.Community.Application.DTOs;

namespace DotnetNiger.Community.Application.Services;

public interface INewsletterService
{
    Task<NewsletterSubscriptionResponse> SubscribeAsync(SubscribeRequest request);
    Task<bool> UnsubscribeAsync(UnsubscribeRequest request);
    Task<PaginatedResponse<NewsletterSubscriptionResponse>> GetAllAsync(int page = 1, int pageSize = 10);
    Task<int> GetActiveCountAsync();
}
