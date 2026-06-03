using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DotnetNiger.Identity.Application.DTOs;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]

[Route("api/v{version:apiVersion}/{tenantId:guid}/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService) => _userService = userService;

    /// <summary>Crée un nouvel utilisateur dans le tenant spécifié.</summary>
    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(Guid tenantId, [FromBody] CreateUserRequest request)
    {
        if (request.TenantId != tenantId)
            return BadRequest(new ErrorResponse("Le tenant de l'URL ne correspond pas à la requête"));

        var user = await _userService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { tenantId, id = user.Id }, user);
    }

    /// <summary>Retourne un utilisateur par son ID (isolé par tenant).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid tenantId, Guid id)
    {
        var user = await _userService.GetByIdAsync(tenantId, id);
        if (user == null) return NotFound(new ErrorResponse("Utilisateur non trouvé"));
        return Ok(user);
    }

    /// <summary>Liste tous les utilisateurs du tenant (paginated).</summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UserResponse>>> GetAll(Guid tenantId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetByTenantAsync(tenantId, new PaginationQuery(page, pageSize));
        return Ok(result);
    }

    /// <summary>Met à jour un utilisateur (prénom, nom, avatar, statut).</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update(Guid tenantId, Guid id,
        [FromBody] UpdateUserRequest request)
    {
        var user = await _userService.UpdateAsync(tenantId, id, request);
        return Ok(user);
    }

    /// <summary>Supprime définitivement un utilisateur du tenant.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid tenantId, Guid id)
    {
        await _userService.DeleteAsync(tenantId, id);
        return NoContent();
    }

    /// <summary>Change le mot de passe d'un utilisateur.</summary>
    [HttpPost("{id:guid}/change-password")]
    public async Task<ActionResult<UserResponse>> ChangePassword(Guid tenantId, Guid id,
        [FromBody] ChangePasswordRequest request)
    {
        var user = await _userService.ChangePasswordAsync(tenantId, id, request);
        return Ok(user);
    }

    /// <summary>Envoie un email de réinitialisation de mot de passe à un utilisateur.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _userService.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "Un lien de réinitialisation a été envoyé par email." });
    }
}
