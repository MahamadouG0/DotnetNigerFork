using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Application.Services;

namespace DotnetNiger.Identity.Pages.Account;

[EnableRateLimiting("Auth")]
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender<ApplicationUser> _emailSender;

    public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender<ApplicationUser> emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Veuillez saisir votre adresse email.";
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Email);
        if (user == null || !user.IsActive)
        {
            // Don't reveal whether the email exists or not
            SuccessMessage = "Si cette adresse email existe, vous recevrez un lien de réinitialisation.";
            return Page();
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var resetLink = Url.Page("/Account/ResetPassword", null,
            new { code, email = user.Email }, Request.Scheme) ?? "";

        await _emailSender.SendPasswordResetLinkAsync(user, user.Email!, resetLink);

        SuccessMessage = "Un lien de réinitialisation vous a été envoyé par email.";
        return Page();
    }
}
