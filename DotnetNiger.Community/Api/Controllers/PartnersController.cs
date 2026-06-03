using Asp.Versioning;
using DotnetNiger.Community.Application.DTOs;
using DotnetNiger.Community.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetNiger.Community.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class PartnersController(IPartnerService partnerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? partnerType)
    {
        var partners = await partnerService.GetAllActiveAsync(partnerType);
        return Ok(new { Success = true, Data = partners });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var p = await partnerService.GetByIdAsync(id);
        if (p is null) return NotFound(new { Success = false, Message = "Partenaire non trouvé" });
        return Ok(new { Success = true, Data = p });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreatePartnerRequest request)
    {
        var partner = await partnerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = partner.Id }, new { Success = true, Data = partner });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePartnerRequest request)
    {
        var p = await partnerService.UpdateAsync(id, request);
        if (p is null) return NotFound(new { Success = false, Message = "Partenaire non trouvé" });
        return Ok(new { Success = true, Data = p });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await partnerService.DeleteAsync(id);
        if (!deleted) return NotFound(new { Success = false, Message = "Partenaire non trouvé" });
        return Ok(new { Success = true, Message = "Partenaire supprimé" });
    }
}
