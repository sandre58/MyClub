// -----------------------------------------------------------------------
// <copyright file="DomainEventTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Common;

public sealed class DomainEventTests
{
    [Fact]
    public void Domain_event_stores_the_occurred_on_timestamp_from_clock()
    {
        // Arrange
        var occurredOn = new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero);
        var clock = new FakeClock(occurredOn);

        // Act
        var domainEvent = new TestDomainEvent(clock, "test");

        // Assert
        domainEvent.OccurredOn.Should().Be(occurredOn);
    }

    [Fact]
    public void Domain_event_rejects_null_clock()
    {
        // Act
        var act = () => new TestDomainEvent(null!, "test");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
