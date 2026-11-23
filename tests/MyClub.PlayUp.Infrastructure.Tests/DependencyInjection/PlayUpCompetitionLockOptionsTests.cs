// -----------------------------------------------------------------------
// <copyright file="PlayUpCompetitionLockOptionsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Infrastructure.DependencyInjection;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.DependencyInjection;

public sealed class PlayUpCompetitionLockOptionsTests
{
    [Theory]
    [InlineData("00:00:03", 3000)]
    [InlineData("500", 500)]
    public void Parse_accepts_time_span_and_milliseconds(string raw, long expectedMilliseconds)
    {
        var options = PlayUpCompetitionLockOptions.Parse(raw);
        options.LockTimeout.TotalMilliseconds.Should().Be(expectedMilliseconds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_blank_value_uses_default(string? raw)
    {
        var options = PlayUpCompetitionLockOptions.Parse(raw);
        options.LockTimeout.Should().Be(PlayUpCompetitionLockOptions.DefaultLockTimeout);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("00:00:00")]
    [InlineData("-1")]
    public void Parse_rejects_non_positive_values(string raw)
    {
        var act = () => PlayUpCompetitionLockOptions.Parse(raw);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateAgainstCommandTimeout_rejects_lock_timeout_equal_to_command_timeout()
    {
        var options = new PlayUpCompetitionLockOptions { LockTimeout = TimeSpan.FromSeconds(30) };
        var act = () => options.ValidateAgainstCommandTimeout(30);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddPlayUpInfrastructure_rejects_lock_timeout_not_strictly_below_command_timeout()
    {
        var services = new ServiceCollection();
        var act = () => services.AddPlayUpInfrastructure(
            "Host=127.0.0.1;Database=x;Username=x;Password=x",
            competitionLockOptions: new PlayUpCompetitionLockOptions { LockTimeout = TimeSpan.FromSeconds(30) },
            npgsqlCommandTimeoutSeconds: 30);
        act.Should().Throw<InvalidOperationException>();
    }
}
