using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Infrastructure;

/// <summary>
/// Sends every authenticated request to <c>/Legal/AcceptareTermeni</c> until the account has
/// accepted the current Terms and Conditions (<see cref="ApplicationUser.TermsAcceptedAt"/>).
/// Registered globally (see Program.cs), so there is nowhere a signed-in user with unaccepted
/// terms can land except that one page — the same "can't skip this" pattern Google uses for its
/// own terms updates. Pages under <c>/Legal</c> and the Identity area (login/register/logout,
/// which run before a session even exists) are exempt so the acceptance page itself, and signing
/// out of it, both stay reachable.
/// </summary>
public sealed class RequireTermsAcceptedFilter : IAsyncPageFilter
{
    private readonly UserManager<ApplicationUser> _userManager;

    public RequireTermsAcceptedFilter(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var path = httpContext.Request.Path;
            if (!path.StartsWithSegments("/Legal") && !path.StartsWithSegments("/Identity"))
            {
                var user = await _userManager.GetUserAsync(httpContext.User);
                if (user is not null && user.TermsAcceptedAt is null)
                {
                    var returnUrl = httpContext.Request.Path + httpContext.Request.QueryString;
                    context.Result = new RedirectToPageResult("/Legal/AcceptareTermeni", new { returnUrl });
                    return;
                }
            }
        }

        await next();
    }
}
