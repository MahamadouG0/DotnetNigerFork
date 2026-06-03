using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages;

[AllowAnonymous]
public class SecuriteModel : PageModel
{
    public void OnGet() { }
}
