using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages;

[AllowAnonymous]
public class SupportModel : PageModel
{
    public void OnGet() { }
}
