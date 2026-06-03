using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using DotnetNiger.Identity.Domain.Entities;
using DotnetNiger.Identity.Infrastructure;

namespace DotnetNiger.Identity.Pages.Account;

[EnableRateLimiting("Auth")]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IdentityDbContext _db;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IdentityDbContext db,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    [BindProperty]
    public string FirstName { get; set; } = string.Empty;

    [BindProperty]
    public string LastName { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public List<ExternalProvider> ExternalProviders { get; set; } = [];

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            Response.Redirect(string.IsNullOrEmpty(ReturnUrl) ? "/" : ReturnUrl);
            return;
        }

        ReturnUrl ??= "/";
        var schemes = await _signInManager.GetExternalAuthenticationSchemesAsync();
        ExternalProviders = schemes
            .Where(s => !string.IsNullOrEmpty(s.DisplayName))
            .Select(s => new ExternalProvider { Name = s.Name, DisplayName = s.DisplayName! })
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ReturnUrl ??= "/";

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Les mots de passe ne correspondent pas.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ErrorMessage = "Le prénom et le nom sont requis.";
            return Page();
        }

        var existingUser = await _userManager.FindByEmailAsync(Email);
        if (existingUser != null)
        {
            ErrorMessage = "Un compte avec cet email existe déjà.";
            return Page();
        }

        var tenant = await _db.Tenants.FirstOrDefaultAsync();
        if (tenant == null)
        {
            ErrorMessage = "Aucun tenant configuré. Veuillez contacter l'administrateur.";
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = Email,
            Email = Email,
            FirstName = FirstName,
            LastName = LastName,
            TenantId = tenant.Id,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            ErrorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            return Page();
        }

        await _userManager.AddToRoleAsync(user, "User");
        _logger.LogInformation("New user registered: {Email}", Email);

        if (ReturnUrl == "/" || string.IsNullOrEmpty(ReturnUrl))
        {
            SuccessMessage = "Compte créé avec succès ! Vous pouvez maintenant vous connecter.";
            return Page();
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(ReturnUrl);
    }
}

public class ExternalProvider
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
