// -----------------------------------------------------------------------
// <copyright file="SystemClockTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Infrastructure.Time;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Time;

public sealed class SystemClockTests
{
    [Fact]
    public void UtcNow_is_utc()
    {
        var clock = new SystemClock();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var now = clock.UtcNow;

        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        now.Offset.Should().Be(TimeSpan.Zero);
        now.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
