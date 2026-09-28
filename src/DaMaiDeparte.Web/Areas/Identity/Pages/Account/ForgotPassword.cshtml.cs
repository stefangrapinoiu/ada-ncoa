using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace DaMaiDeparte.Web.Areas.Identity.Pages.Account;

/// <summary>
/// Generates a password-reset token and e-mails a link to /Account/ResetPassword. Always shows
/// the same "check your e-mail" confirmation whether or not the address has an account, so the
/// page can't be used to find out which e-mail addresses are registered.
/// </summary>
public class ForgotPasswordModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<ForgotPasswordModel> logger)
    {
        _userManager = userManager;
        _emailSender = emailSender;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = UiText.Validation.EmailRequired)]
    [EmailAddress(ErrorMessage = UiText.Validation.EmailInvalid)]
    [Display(Name = "Adresă de e-mail")]
    public string Email { get; set; } = string.Empty;

    public bool Submitted { get; private set; }

    /// <summary>
    /// Set only in Development, and only when no SMTP server is configured, so the reset link
    /// can be tested locally without digging through container logs. Never set in Production.
    /// </summary>
    public string? DevResetLink { get; private set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Email.Trim());
        if (user is not null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var resetUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = encodedToken, email = user.Email },
                protocol: Request.Scheme);

            if (resetUrl is not null)
            {
                try
                {
                    await _emailSender.SendAsync(
                        user.Email!,
                        $"Resetează-ți parola pe {AppInfo.Name}",
                        BuildResetEmailBody(user.FirstName, resetUrl));
                }
                catch (Exception ex)
                {
                    // The visible outcome stays the generic "check your e-mail" message either
                    // way, so a delivery failure never leaks whether the address has an account.
                    _logger.LogError(ex, "Failed to send password reset e-mail");
                }

                if (_environment.IsDevelopment() && string.IsNullOrWhiteSpace(_configuration["Email:Host"]))
                {
                    DevResetLink = resetUrl;
                }
            }
        }

        Submitted = true;
        return Page();
    }

    private static string BuildResetEmailBody(string firstName, string resetUrl) =>
        $"""
        <p>Bună, {WebUtility.HtmlEncode(firstName)},</p>
        <p>Am primit o cerere de resetare a parolei pentru contul tău {AppInfo.Name}.</p>
        <p><a href="{resetUrl}">Apasă aici pentru a-ți alege o parolă nouă</a>.</p>
        <p>Dacă nu ai cerut tu această resetare, poți ignora acest e-mail — contul tău rămâne neschimbat.</p>
        """;
}
