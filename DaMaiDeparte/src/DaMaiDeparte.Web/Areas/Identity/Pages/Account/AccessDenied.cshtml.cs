using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

public class AccessDeniedModel : PageModel
{
    public void OnGet()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
    }
}
