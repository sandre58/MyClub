// -----------------------------------------------------------------------
// <copyright file="HostListenExposureWarnings.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace MyClub.PlayUp.Host;

internal static partial class HostListenExposureWarnings
{
    internal const string ExposureGuidePath = "docs/guides/host-exposure.md";

    public static void RegisterNonLoopbackListenUrlWarningsIfDevelopment(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("HostExposure");
            var serverAddresses = app.Services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>()?.Addresses;
            var listenUrls = serverAddresses is { Count: > 0 } ? serverAddresses : app.Urls;
            LogNonLoopbackListenUrls(logger, listenUrls);
        });
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
