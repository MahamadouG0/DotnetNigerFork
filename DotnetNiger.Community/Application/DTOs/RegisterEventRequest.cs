using System.ComponentModel.DataAnnotations;

namespace DotnetNiger.Community.Application.DTOs;

public class RegisterEventRequest
{
    [Required]
    public Guid EventId { get; set; }
}
