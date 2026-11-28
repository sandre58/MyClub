// -----------------------------------------------------------------------
// <copyright file="MatchResultConfrontationTieTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

/// <summary>
/// <see cref="MatchResult.CreateConfrontationTiePenaltyShootout"/> invariants and contrast with the public constructor.
/// Unequal play score with a shootout on the public constructor remains covered by <see cref="MatchResultTests"/>.
/// </summary>
public sealed class MatchResultConfrontationTieTests
{
    [Fact]
    public void CreateConfrontationTiePenaltyShootout_accepts_unequal_score_with_decisive_shootout()
    {
        var result = MatchResult.CreateConfrontationTiePenaltyShootout(
            new Score(1, 0),
            extraTimePlayed: false,
            new PenaltyShootoutScore(5, 4));

        result.Type.Should().Be(ResultType.Played);
        result.Score.Should().Be(new Score(1, 0));
        result.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(5, 4));
        result.ConfrontationTiePenaltyShootout.Should().BeTrue();
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_equal_play_score()
    {
        var act = () => MatchResult.CreateConfrontationTiePenaltyShootout(
            new Score(1, 1),
            extraTimePlayed: true,
            new PenaltyShootoutScore(5, 4));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_non_decisive_shootout()
    {
        var act = () => MatchResult.CreateConfrontationTiePenaltyShootout(
            new Score(2, 1),
            extraTimePlayed: false,
            new PenaltyShootoutScore(3, 3));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void Public_constructor_still_rejects_unequal_play_score_with_shootout()
    {
        var act = () => new MatchResult(
            ResultType.Played,
            new Score(2, 1),
            penaltyShootoutScore: new PenaltyShootoutScore(5, 4));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }
}