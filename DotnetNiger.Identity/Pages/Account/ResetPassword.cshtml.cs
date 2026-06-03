using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using DotnetNiger.Identity.Domain.Entities;

namespace DotnetNiger.Identity.Pages.Account;

[EnableRateLimiting("Auth")]
public class ResetPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ResetPasswordModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string? Code { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Email { get; set; }

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(Code) || string.IsNullOrEmpty(Email))
            return RedirectToPage("/Account/ForgotPassword");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Les mots de passe ne correspondent pas.";
            return Page();
        }

        if (string.IsNullOrEmpty(Code) || string.IsNullOrEmpty(Email))
            return RedirectToPage("/Account/ForgotPassword");

        var user = await _userManager.FindByEmailAsync(Email);
        if (user == null)
            return RedirectToPage("/Account/ResetPasswordConfirmation");

        var result = await _userManager.ResetPasswordAsync(user, Code, Password);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            return Page();
        }

        return RedirectToPage("/Account/ResetPasswordConfirmation");
    }
}
