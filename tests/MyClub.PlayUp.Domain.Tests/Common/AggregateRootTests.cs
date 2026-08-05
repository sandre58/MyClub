// -----------------------------------------------------------------------
// <copyright file="AggregateRootTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Common;

public sealed class AggregateRootTests
{
    [Fact]
    public void Raise_collects_domain_events_in_FIFO_order()
    {
        // Arrange
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero));
        var aggregate = new TestAggregate(CompetitionId.New());
        var first = new TestDomainEvent(clock, "first");
        var second = new TestDomainEvent(clock, "second");

        // Act
        aggregate.DoSomething(first);
        aggregate.DoSomething(second);

        // Assert
        aggregate.DomainEvents.Should().ContainInOrder(first, second);
    }

    [Fact]
    public void ClearDomainEvents_removes_all_uncommitted_events()
    {
        // Arrange
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero));
        var aggregate = new TestAggregate(CompetitionId.New());
        aggregate.DoSomething(new TestDomainEvent(clock, "event"));

        // Act
        aggregate.ClearDomainEvents();

        // Assert
        aggregate.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void DomainEvents_collection_cannot_be_mutated_from_outside()
    {
        // Arrange
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 5, 10, 0, 0, TimeSpan.Zero));
        var aggregate = new TestAggregate(CompetitionId.New());
        aggregate.DoSomething(new TestDomainEvent(clock, "event"));

        // Act
        var act = () => ((ICollection<IDomainEvent>)aggregate.DomainEvents)
            .Add(new TestDomainEvent(clock, "intruder"));

        // Assert
        act.Should().Throw<NotSupportedException>();
        aggregate.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void Raise_rejects_null_domain_event()
    {
        // Arrange
        var aggregate = new TestAggregate(CompetitionId.New());

        // Act
        var act = () => aggregate.DoSomething(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
