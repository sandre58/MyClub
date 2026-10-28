// -----------------------------------------------------------------------
// <copyright file="HostExposureWarningTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace MyClub.PlayUp.Host.Tests;

/// <summary>
/// Bind exposure warnings and AllowedHosts regression.
/// </summary>
public sealed class HostExposureWarningTests
{
    [Fact]
    public void T1_loopback_listen_urls_do_not_emit_exposure_warning()
    {
        var sink = new LogMessageSink();
        var logger = sink.CreateLogger("HostExposure");

        HostListenExposureGuard.LogNonLoopbackListenUrls(
            logger,
            ["http://127.0.0.1:5287", "http://localhost:5287"]);

        sink.WarningMessages.Should().BeEmpty();
    }

    [Fact]
    public void T2_non_loopback_listen_url_emits_actionable_warning()
    {
        var sink = new LogMessageSink();
        var logger = sink.CreateLogger("HostExposure");
        const string listenUrl = "http://0.0.0.0:5287";

        HostListenExposureGuard.LogNonLoopbackListenUrls(logger, [listenUrl]);

        sink.WarningMessages.Should().ContainSingle();
        var message = sink.WarningMessages[0];
        message.Should().Contain(listenUrl);
        message.Should().Contain("other network interfaces");
        message.Should().Contain(HostListenExposureGuard.ExposureGuidePath);
        message.Should().Contain("launchSettings");
        message.Should().NotContain("Internet exposure detected");
        message.Should().NotContain("public Internet");
    }

    [Fact]
    public void T6_production_refuses_non_loopback_listen_without_opt_in()
    {
        var configuration = new ConfigurationBuilder().Build();

        HostListenExposureGuard
            .ShouldRefuseNonLoopbackListen(configuration, ["http://0.0.0.0:5287"])
            .Should()
            .BeTrue();
    }

    [Fact]
    public void T7_production_allows_non_loopback_when_opt_in_configured()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [HostListenExposureGuard.AllowNonLoopbackBindConfigKey] = "true"
                })
            .Build();

        HostListenExposureGuard
            .ShouldRefuseNonLoopbackListen(configuration, ["http://0.0.0.0:5287"])
            .Should()
            .BeFalse();
    }

    private sealed class LogMessageSink : ILoggerProvider
    {
        public List<string> WarningMessages { get; } = [];

        public ILogger CreateLogger(string categoryName) => new ForwardingLogger(this);

        public void Dispose()
        {
        }

        private sealed class ForwardingLogger(LogMessageSink sink) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state)
                where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (logLevel != LogLevel.Warning)
                {
                    return;
                }

                sink.WarningMessages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
