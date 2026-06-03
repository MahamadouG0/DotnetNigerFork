using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DotnetNiger.Identity.Web.Pages.Developer;

[Authorize]
public class DocsModel : PageModel
{
    public string Lang { get; set; } = "fr";
    public string GatewayUrl { get; private set; } = "http://localhost:5000";
    public string IdentityUrl { get; private set; } = "http://localhost:5075";

    public void OnGet(string? lang)
    {
        Lang = lang switch
        {
            "en" => "en",
            _ => "fr"
        };
        GatewayUrl = (HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["DeveloperPortal:GatewayBaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
        IdentityUrl = (HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Identity:BaseUrl"]!).TrimEnd('/');
    }
}
