using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    public IActionResult OnGet()
    {
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = "/"
        }, "OpenIdConnect");
    }
}
