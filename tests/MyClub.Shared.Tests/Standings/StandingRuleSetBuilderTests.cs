// -----------------------------------------------------------------------
// <copyright file="StandingRuleSetBuilderTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Moq;
using MyClub.Shared.Domain.Enums;
using MyClub.Shared.Domain.Standings.Comparers;
using MyClub.Shared.Domain.Standings.Rules;
using Xunit;

namespace MyClub.Shared.Tests.Standings;

public class StandingRuleSetBuilderTests
{
    [Fact]
    public void WithPoints_ShouldSetPointsForResultType()
    {
        var builder = new StandingRuleSetBuilder()
            .WithPoints(MatchResultType.Win, 5)
            .WithPoints(MatchResultType.Draw, 2);

        var ruleSet = builder.Build();

        ruleSet.PointsByOutcome[MatchResultType.Win].Should().Be(5);
        ruleSet.PointsByOutcome[MatchResultType.Draw].Should().Be(2);
    }

    [Fact]
    public void AddColumn_ShouldAddCustomColumn()
    {
        var mockColumn = new Mock<IStandingColumn>();
        mockColumn.SetupGet(x => x.Key).Returns("Custom");

        var builder = new StandingRuleSetBuilder()
            .AddColumn(mockColumn.Object);

        var ruleSet = builder.Build();

        ruleSet.Columns.Should().Contain(mockColumn.Object);
    }

    [Fact]
    public void AddColumn_ByEnum_ShouldAddColumn()
    {
        var builder = new StandingRuleSetBuilder()
            .AddColumn(StandingColumnType.GamesWon);

        var ruleSet = builder.Build();

        ruleSet.Columns.Should().ContainSingle(c => c.Key == "GamesWon");
    }

    [Fact]
    public void WithDefaultColumns_ShouldAddAllDefaultColumns()
    {
        var builder = new StandingRuleSetBuilder()
            .WithDefaultColumns();

        var ruleSet = builder.Build();

        ruleSet.Columns.Should().BeEquivalentTo(StandingRuleSet.DefaultColumns);
    }

    [Fact]
    public void WithComparer_ShouldSetCustomComparer()
    {
        var customComparer = new StandingComparerBuilder().ThenByPoints().Build();
        var builder = new StandingRuleSetBuilder()
            .WithComparer(customComparer);

        var ruleSet = builder.Build();

        ruleSet.Comparer.Should().BeEquivalentTo(customComparer);
    }

    [Fact]
    public void Build_ShouldCombineAllSettings()
    {
        var customComparer = new StandingComparerBuilder().ThenByPoints().Build();
        var builder = new StandingRuleSetBuilder()
            .WithPoints(MatchResultType.Win, 10)
            .AddColumn(StandingColumnType.GamesWon)
            .WithComparer(customComparer);

        var ruleSet = builder.Build();

        ruleSet.PointsByOutcome[MatchResultType.Win].Should().Be(10);
        ruleSet.Columns.Should().ContainSingle(c => c.Key == "GamesWon");
        ruleSet.Comparer.Should().BeEquivalentTo(customComparer);
    }
}
