// -----------------------------------------------------------------------
// <copyright file="HostListenExposureWarnings.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;

namespace MyClub.PlayUp.Host;

internal static partial class HostListenExposureWarnings
{
    internal const string ExposureGuidePath = "docs/guides/host-exposure.md";

    public static void LogNonLoopbackListenUrlsIfDevelopment(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("HostExposure");
        LogNonLoopbackListenUrls(logger, app.Urls);
    }

    internal static void LogNonLoopbackListenUrls(ILogger logger, IEnumerable<string> listenUrls)
    {
        foreach (var listenUrl in listenUrls)
        {
            if (!Uri.TryCreate(listenUrl, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (IsLoopbackHost(uri.Host))
            {
                continue;
            }

            LogNonLoopbackListenUrl(logger, listenUrl);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Play'Up Host is listening on a non-loopback address ({ListenUrl}). "
            + "Clients on other network interfaces may reach the anonymous Play'Up and Media APIs. "
            + "This warning does not prove WAN reachability (firewall, NAT, and port forwarding are operator concerns). "
            + "Review bind settings: dotnet run --urls, ASPNETCORE_URLS, or Properties/launchSettings.json applicationUrl. "
            + "See docs/guides/host-exposure.md.")]
    private static partial void LogNonLoopbackListenUrl(ILogger logger, string listenUrl);

    private static bool IsLoopbackHost(string host) => !string.IsNullOrWhiteSpace(host) && (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address)));
}
