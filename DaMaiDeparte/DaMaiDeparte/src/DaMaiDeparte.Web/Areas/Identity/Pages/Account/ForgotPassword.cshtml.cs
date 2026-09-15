using System.ComponentModel.DataAnnotations;
using DaMaiDeparte.Web.Resources;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

/// <summary>
/// E-mail sending is out of scope for the MVP, so this page only acknowledges the request.
/// Wire up an IEmailSender and a ResetPassword page when e-mail notifications are added.
/// </summary>
public class ForgotPasswordModel : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = UiText.Validation.EmailRequired)]
    [EmailAddress(ErrorMessage = UiText.Validation.EmailInvalid)]
    [Display(Name = "Adresă de e-mail")]
    public string Email { get; set; } = string.Empty;

    public bool Submitted { get; private set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Submitted = true;
        return Page();
    }
}
