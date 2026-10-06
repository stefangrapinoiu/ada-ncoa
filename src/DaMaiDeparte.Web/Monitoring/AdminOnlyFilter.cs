using System.Security.Claims;
using DaMaiDeparte.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DaMaiDeparte.Web.Monitoring;

/// <summary>
/// Guards every page under /Admin. Anyone who is not an admin — signed in or not — gets a plain
/// 404, so the area's existence is not revealed (no login redirect, no 403). Signed-in users who
/// try are recorded in the security log.
/// </summary>
public sealed class AdminOnlyFilter : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var signedIn = user.Identity?.IsAuthenticated == true;

        if (signedIn && user.IsInRole(AdminRole.Name))
        {
            await next();
            return;
        }

        if (signedIn)
        {
            var audit = context.HttpContext.RequestServices.GetService<IAuditLog>();
            if (audit is not null)
            {
                await audit.WriteAsync(
                    AuditEventType.AdminAccessDenied,
                    user.FindFirstValue(ClaimTypes.NameIdentifier),
                    user.Identity?.Name,
                    context.HttpContext.Request.Path);
            }
        }

        context.Result = new NotFoundResult();
    }
}
