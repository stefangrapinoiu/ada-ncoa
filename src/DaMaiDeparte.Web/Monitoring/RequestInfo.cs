using System.Net;
using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Monitoring;

/// <summary>Small helpers for reading who/what made a request, for the security log.</summary>
public static class RequestInfo
{
    /// <summary>
    /// The client's address. Behind Caddy this is the real client, because UseForwardedHeaders
    /// (configured in Program.cs) has already copied X-Forwarded-For into RemoteIpAddress.
    /// </summary>
    public static string? ClientIp(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress;
        if (address is null)
        {
            return null;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.ToString();
    }

    public static DeviceType Device(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return DeviceType.Unknown;
        }

        var ua = userAgent;
        if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)
            || (ua.Contains("Android", StringComparison.OrdinalIgnoreCase) && !ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase)))
        {
            return DeviceType.Tablet;
        }

        if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)
            || ua.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
        {
            return DeviceType.Mobile;
        }

        if (ua.Contains("Windows", StringComparison.OrdinalIgnoreCase)
            || ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)
            || ua.Contains("X11", StringComparison.OrdinalIgnoreCase)
            || ua.Contains("CrOS", StringComparison.OrdinalIgnoreCase))
        {
            return DeviceType.Desktop;
        }

        return DeviceType.Unknown;
    }
}
