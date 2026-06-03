using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer;

[Authorize]
public class SecuriteModel : PageModel
{
    public void OnGet() { }
}
