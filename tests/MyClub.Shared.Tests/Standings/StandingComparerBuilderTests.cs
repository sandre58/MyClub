// -----------------------------------------------------------------------
// <copyright file="StandingComparerBuilderTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Standings.Comparers;
using MyClub.Shared.Domain.Teams;
using MyClub.Tests.Common;
using Xunit;

namespace MyClub.Shared.Tests.Standings;

public class StandingComparerBuilderTests : TestBase
{
    [Fact]
    public void Then_ShouldAddCustomComparer()
    {
        var mockComparer = new Mock<IStandingComparer>();
        var builder = new StandingComparerBuilder();

        var result = builder.Then(mockComparer.Object);

        result.Should().BeSameAs(builder);
        var comparer = builder.Build();
        comparer.Should().NotBeNull();

        // The comparer should use the mockComparer
        var row1 = new DummyStandingRow(TeamId.New().ToReference());
        var row2 = new DummyStandingRow(TeamId.New().ToReference());
        mockComparer.Setup(x => x.Compare(row1, row2)).Returns(1);
        comparer.Compare(row1, row2).Should().Be(1);
    }

    [Fact]
    public void ThenBy_StringColumn_ShouldAddColumnComparer()
    {
        var builder = new StandingComparerBuilder();
        var comparer = builder.ThenBy("Points").Build();

        comparer.Should().NotBeNull();
    }

    [Fact]
    public void ThenBy_EnumColumn_ShouldAddColumnComparer()
    {
        var builder = new StandingComparerBuilder();
        var comparer = builder.ThenBy(StandingColumnType.GoalsFor).Build();

        comparer.Should().NotBeNull();
    }

    [Fact]
    public void ThenBy_Selector_ShouldAddComparableComparer()
    {
        var builder = new StandingComparerBuilder();
        var comparer = builder.ThenBy(x => 42).Build();

        comparer.Should().NotBeNull();
    }

    [Fact]
    public void ThenByHeadToHead_ShouldAddHeadToHeadComparer()
    {
        var builder = new StandingComparerBuilder();
        var comparer = builder.ThenByHeadToHead().Build();

        comparer.Should().NotBeNull();
    }

    [Fact]
    public void ThenByPoints_ShouldAddPointsComparer()
    {
        var builder = new StandingComparerBuilder();
        var comparer = builder.ThenByPoints().Build();

        comparer.Should().NotBeNull();
    }

    [Fact]
    public void Build_ShouldReturnStandingComparerWithAllComparers()
    {
        var builder = new StandingComparerBuilder()
            .ThenByPoints()
            .ThenBy(StandingColumnType.GoalsDifference)
            .ThenByHeadToHead();

        var comparer = builder.Build();

        comparer.Should().NotBeNull();
        comparer.Should().BeOfType<StandingComparer>();
    }
}
