using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DaMaiDeparte.Web.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    private readonly ILogger<ErrorModel> _logger;

    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    public int ErrorCode { get; private set; } = 500;

    public string? RequestId { get; private set; }

    public void OnGet(int? code) => Handle(code);

    public void OnPost(int? code) => Handle(code);

    private void Handle(int? code)
    {
        ErrorCode = code ?? StatusCodes.Status500InternalServerError;
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;

        var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (exception?.Error is not null)
        {
            _logger.LogError(exception.Error, "Unhandled exception for request {RequestId} on {Path}", RequestId, exception.Path);
        }

        if (code.HasValue)
        {
            Response.StatusCode = code.Value;
        }
    }
}
