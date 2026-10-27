// -----------------------------------------------------------------------
// <copyright file="HostListenExposureGuard.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;

namespace MyClub.PlayUp.Host;

/// <summary>
/// Development warns on non-loopback binds; Production refuses them unless opted in.
/// </summary>
internal static partial class HostListenExposureGuard
{
    internal const string ExposureGuidePath = "docs/guides/host-exposure.md";

    internal const string AllowNonLoopbackBindConfigKey = "PlayUp:Host:AllowNonLoopbackBind";

    public static void Register(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            RegisterDevelopmentWarnings(app);
            return;
        }

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var listenUrls = ResolveListenUrls(app);
            if (!ShouldRefuseNonLoopbackListen(app.Configuration, listenUrls))
            {
                return;
            }

            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("HostExposure");
            foreach (var listenUrl in listenUrls.Where(url => IsNonLoopbackListenUrl(url)))
            {
                LogProductionNonLoopbackBindRefused(logger, listenUrl);
            }

            app.Lifetime.StopApplication();
        });
    }

    internal static bool ShouldRefuseNonLoopbackListen(
        IConfiguration configuration,
        IEnumerable<string> listenUrls) =>
        listenUrls.Any(IsNonLoopbackListenUrl)
        && !configuration.GetValue(AllowNonLoopbackBindConfigKey, defaultValue: false);

    internal static void LogNonLoopbackListenUrls(ILogger logger, IEnumerable<string> listenUrls)
    {
        foreach (var listenUrl in listenUrls)
        {
            if (!IsNonLoopbackListenUrl(listenUrl))
            {
                continue;
            }

            LogDevelopmentNonLoopbackListenUrl(logger, listenUrl);
        }
    }

    private static void RegisterDevelopmentWarnings(WebApplication app) =>
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("HostExposure");
            LogNonLoopbackListenUrls(logger, ResolveListenUrls(app));
        });

    private static IEnumerable<string> ResolveListenUrls(WebApplication app)
    {
        var serverAddresses = app.Services.GetService<IServer>()?.Features.Get<IServerAddressesFeature>()?.Addresses;
        return serverAddresses is { Count: > 0 } ? serverAddresses : app.Urls;
    }

    private static bool IsNonLoopbackListenUrl(string listenUrl) =>
        Uri.TryCreate(listenUrl, UriKind.Absolute, out var uri) && IsNonLoopbackHost(uri.Host);

    private static bool IsNonLoopbackHost(string host) =>
        !string.IsNullOrWhiteSpace(host)
        && !string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
        && (!IPAddress.TryParse(host, out var address) || !IPAddress.IsLoopback(address));

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Play'Up Host is listening on a non-loopback address ({ListenUrl}). "
            + "Clients on other network interfaces may reach the anonymous Play'Up and Media APIs. "
            + "This warning does not prove WAN reachability (firewall, NAT, and port forwarding are operator concerns). "
            + "Review bind settings: dotnet run --urls, ASPNETCORE_URLS, or Properties/launchSettings.json applicationUrl. "
            + "See docs/guides/host-exposure.md.")]
    private static partial void LogDevelopmentNonLoopbackListenUrl(ILogger logger, string listenUrl);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Critical,
        Message = "Play'Up Host refused to run in a non-Development environment with non-loopback listen URL ({ListenUrl}). "
            + "Trajectoire C requires loopback bind until authentication and TLS exist for remote access. "
            + "Use loopback URLs, or set PlayUp:Host:AllowNonLoopbackBind=true only with explicit operator acceptance of risk. "
            + "See docs/guides/host-exposure.md.")]
    private static partial void LogProductionNonLoopbackBindRefused(ILogger logger, string listenUrl);
}
