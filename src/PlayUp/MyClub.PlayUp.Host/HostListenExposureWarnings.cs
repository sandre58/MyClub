// -----------------------------------------------------------------------
// <copyright file="HostListenExposureWarnings.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;

namespace MyClub.PlayUp.Host;

internal static partial class HostListenExposureWarnings
{
    public static void LogNonLoopbackListenUrlsIfDevelopment(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("HostExposure");

        foreach (var listenUrl in app.Urls)
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
            + "Untrusted clients on the network may reach anonymous Play'Up and Media APIs. ")]
    private static partial void LogNonLoopbackListenUrl(ILogger logger, string listenUrl);

    private static bool IsLoopbackHost(string host) => !string.IsNullOrWhiteSpace(host) && (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) || (IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address)));
}
