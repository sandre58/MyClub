// -----------------------------------------------------------------------
// <copyright file="TieFormatTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class TieFormatTests
{
    [Fact]
    public void Constructor_accepts_single_leg_final()
    {
        // Arrange & Act
        var format = new TieFormat(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            extraTimeRule: new ExtraTimeRule(),
            penaltyShootoutRule: new PenaltyShootoutRule());

        // Assert
        format.NumberOfLegs.Should().Be(1);
        format.AggregateScoring.Should().BeFalse();
        format.AwayGoalsRule.Should().BeNull();
        format.ExtraTimeRule.Should().NotBeNull();
        format.PenaltyShootoutRule.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_accepts_two_legged_tie_with_away_goals()
    {
        // Arrange & Act
        var format = new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: new AwayGoalsRule());

        // Assert
        format.NumberOfLegs.Should().Be(2);
        format.AggregateScoring.Should().BeTrue();
        format.AwayGoalsRule.Should().NotBeNull();
    }

    [Fact]
    public void Equality_is_structural()
    {
        // Arrange & Act
        var left = new TieFormat(2, true, new AwayGoalsRule());
        var right = new TieFormat(2, true, new AwayGoalsRule());

        // Assert
        left.Should().Be(right);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Constructor_rejects_invalid_leg_count(int legs)
    {
        // Arrange & Act
        var act = () => new TieFormat(legs, aggregateScoring: false);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.TieFormatInvalid);
    }

    [Fact]
    public void Constructor_rejects_aggregate_scoring_on_single_leg()
    {
        // Arrange & Act
        var act = () => new TieFormat(TieFormat.SingleLeg, aggregateScoring: true);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.TieFormatInvalid);
    }

    [Fact]
    public void Constructor_rejects_two_legs_without_aggregate_scoring()
    {
        var act = () => new TieFormat(TieFormat.TwoLegs, aggregateScoring: false);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.TieFormatInvalid);
    }

    [Fact]
    public void Constructor_rejects_away_goals_without_aggregate_two_legs()
    {
        // Arrange & Act
        var act = () => new TieFormat(
            TieFormat.TwoLegs,
            aggregateScoring: false,
            awayGoalsRule: new AwayGoalsRule());

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.TieFormatInvalid);
    }

    [Fact]
    public void Copy_creates_independent_marker_instances()
    {
        // Arrange
        var original = new TieFormat(2, true, new AwayGoalsRule(), new ExtraTimeRule());

        // Act
        var copy = original.Copy();

        // Assert
        copy.Should().Be(original);
        ReferenceEquals(copy.AwayGoalsRule, original.AwayGoalsRule).Should().BeFalse();
        ReferenceEquals(copy.ExtraTimeRule, original.ExtraTimeRule).Should().BeFalse();
    }

    [Fact]
    public void OrDefaultOneLeg_returns_default_when_null()
    {
        TieFormat.OrDefaultOneLeg(null).Should().Be(TieFormat.DefaultOneLeg);
        TieFormat.DefaultOneLeg.NumberOfLegs.Should().Be(TieFormat.SingleLeg);
        TieFormat.DefaultOneLeg.AggregateScoring.Should().BeFalse();
    }

    [Fact]
    public void OrDefaultOneLeg_preserves_explicit_format()
    {
        var twoLegs = new TieFormat(TieFormat.TwoLegs, aggregateScoring: true);
        TieFormat.OrDefaultOneLeg(twoLegs).Should().BeSameAs(twoLegs);
    }
}
