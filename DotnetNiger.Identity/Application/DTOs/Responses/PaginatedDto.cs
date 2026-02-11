// DTO response Identity: PaginatedDto
namespace DotnetNiger.Identity.Application.DTOs.Responses;

// Reponse paginee standard.
public class PaginatedDto<T>
{
	public IReadOnlyList<T> Items { get; set; } = new List<T>();
	public int TotalCount { get; set; }
	public int Skip { get; set; }
	public int Take { get; set; }
}
