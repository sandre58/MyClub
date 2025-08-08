// -----------------------------------------------------------------------
// <copyright file="MatchScopeTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using AutoFixture;
using FluentAssertions;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Scorer.Domain.Primitives;
using MyClub.Shared.Domain.Matches;
using Xunit;

namespace MyClub.Scorer.Domain.Tests.CompetitionAggregate.Stages;

public class MatchScopeTests
{
    [Fact]
    public void Constructor_ShouldSetOriginDate()
    {
        var date = new Fixture().Create<DateTime>();
        var id = MatchdayId.New();
        var container = new DummyMatchScope(id, date);

        container.OriginDate.Should().Be(date);
        container.Date.Should().Be(date);
        container.IsPostponed.Should().BeFalse();
    }

    [Fact]
    public void Postpone_ShouldSetIsPostponedAndChangeDate()
    {
        var date = new Fixture().Create<DateTime>();
        var id = MatchdayId.New();
        var container = new DummyMatchScope(id, date);
        var postponedDate = date.AddDays(2);

        container.Postpone(postponedDate);

        container.IsPostponed.Should().BeTrue();
        container.Date.Should().Be(postponedDate);
    }

    [Fact]
    public void Schedule_ShouldResetPostponedAndSetOriginDate()
    {
        var date = new Fixture().Create<DateTime>();
        var id = MatchdayId.New();
        var container = new DummyMatchScope(id, date);
        var newDate = date.AddDays(5);

        container.Postpone(date.AddDays(1));
        container.Schedule(newDate);

        container.IsPostponed.Should().BeFalse();
        container.Date.Should().Be(newDate);
        container.OriginDate.Should().Be(newDate);
    }

    [Fact]
    public void AddMatch_ShouldAddMatchId()
    {
        var date = new Fixture().Create<DateTime>();
        var id = MatchdayId.New();
        var container = new DummyMatchScope(id, date);
        var matchId = MatchId.New();

        var result = container.AddMatch(matchId);

        result.IsSuccess.Should().BeTrue();
        container.Matches.Should().Contain(matchId);
    }

    [Fact]
    public void AddMatch_ShouldNotAddDuplicateMatchId()
    {
        var date = new Fixture().Create<DateTime>();
        var id = MatchdayId.New();
        var container = new DummyMatchScope(id, date);
        var matchId = MatchId.New();

        container.AddMatch(matchId);
        var result = container.AddMatch(matchId);

        result.IsFailure.Should().BeTrue();
        container.Matches.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveMatch_ShouldRemoveMatchId()
    {
        var date = new Fixture().Create<DateTime>();
        var id = MatchdayId.New();
        var container = new DummyMatchScope(id, date);
        var matchId = MatchId.New();

        container.AddMatch(matchId);
        var removed = container.RemoveMatch(matchId);

        removed.Should().BeTrue();
        container.Matches.Should().NotContain(matchId);
    }

    [Fact]
    public void CompareTo_ShouldCompareByOriginDate()
    {
        var date1 = new Fixture().Create<DateTime>();
        var date2 = date1.AddDays(1);
        var id1 = MatchdayId.New();
        var id2 = MatchdayId.New();
        var container1 = new DummyMatchScope(id1, date1);
        var container2 = new DummyMatchScope(id2, date2);

        container1.CompareTo(container2).Should().BeLessThan(0);
        container2.CompareTo(container1).Should().BeGreaterThan(0);
        container1.CompareTo(container1).Should().Be(0);
    }
}

internal sealed class DummyMatchScope(MatchdayId id, DateTime date) : MatchScope<MatchdayId>(id, date);
