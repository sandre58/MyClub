// -----------------------------------------------------------------------
// <copyright file="MatchResultTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Match;

public sealed class MatchResultTests
{
    [Fact]
    public void Constructor_accepts_valid_type_and_score()
    {
        var result = new MatchResult(ResultType.Played, new Score(2, 1));

        result.Type.Should().Be(ResultType.Played);
        result.Score.Should().Be(new Score(2, 1));
        result.ExtraTimePlayed.Should().BeFalse();
        result.PenaltyShootoutScore.Should().BeNull();
    }

    [Fact]
    public void Constructor_rejects_undefined_result_type()
    {
        var act = () => new MatchResult((ResultType)999, new Score(0, 0));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Theory]
    [InlineData(ResultType.Forfeit)]
    [InlineData(ResultType.WalkOver)]
    [InlineData(ResultType.Administrative)]
    public void Constructor_accepts_all_defined_result_types(ResultType type)
    {
        var result = new MatchResult(type, new Score(3, 0));

        result.Type.Should().Be(type);
    }

    [Fact]
    public void Equality_is_structural()
    {
        var left = new MatchResult(ResultType.Played, new Score(2, 1), extraTimePlayed: true);
        var right = new MatchResult(ResultType.Played, new Score(2, 1), extraTimePlayed: true);
        var different = new MatchResult(ResultType.Forfeit, new Score(3, 0));

        left.Should().Be(right);
        left.Should().NotBe(different);
    }

    [Fact]
    public void Constructor_accepts_extra_time_win_without_shootout()
    {
        var result = new MatchResult(
            ResultType.Played,
            new Score(2, 1),
            extraTimePlayed: true);

        result.ExtraTimePlayed.Should().BeTrue();
        result.PenaltyShootoutScore.Should().BeNull();
    }

    [Fact]
    public void Constructor_accepts_draw_with_decisive_shootout_without_extra_time()
    {
        var result = new MatchResult(
            ResultType.Played,
            new Score(0, 0),
            extraTimePlayed: false,
            new PenaltyShootoutScore(5, 4));

        result.ExtraTimePlayed.Should().BeFalse();
        result.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(5, 4));
    }

    [Fact]
    public void Constructor_accepts_extra_time_draw_with_decisive_shootout()
    {
        var result = new MatchResult(
            ResultType.Played,
            new Score(2, 2),
            extraTimePlayed: true,
            new PenaltyShootoutScore(4, 3));

        result.ExtraTimePlayed.Should().BeTrue();
        result.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(4, 3));
    }

    [Fact]
    public void Constructor_rejects_shootout_when_play_score_is_unequal()
    {
        var act = () => new MatchResult(
            ResultType.Played,
            new Score(2, 1),
            penaltyShootoutScore: new PenaltyShootoutScore(4, 3));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void Constructor_rejects_equal_shootout_score()
    {
        var act = () => new MatchResult(
            ResultType.Played,
            new Score(1, 1),
            penaltyShootoutScore: new PenaltyShootoutScore(3, 3));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }
}
