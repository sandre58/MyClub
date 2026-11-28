// -----------------------------------------------------------------------
// <copyright file="MatchResultConfrontationTieTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Matches;

/// <summary>
/// <see cref="MatchResult.CreateConfrontationTiePenaltyShootout"/> invariants and contrast with the public constructor.
/// Unequal play score with a shootout on the public constructor remains covered by <see cref="MatchResultTests"/>.
/// </summary>
public sealed class MatchResultConfrontationTieTests
{
    private readonly EntryId _entryA = EntryId.New();
    private readonly EntryId _entryB = EntryId.New();

    private static TieFormat TwoLegFormat(bool awayGoals = false, bool withShootout = true) =>
        new(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: awayGoals ? new AwayGoalsRule() : null,
            penaltyShootoutRule: withShootout ? new PenaltyShootoutRule() : null);

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_accepts_unequal_return_with_level_aggregate()
    {
        var tieFormat = TwoLegFormat();
        var result = CreateFlagged(tieFormat, firstScore: new Score(1, 0), returnScore: new Score(1, 0));

        result.Type.Should().Be(ResultType.Played);
        result.Score.Should().Be(new Score(1, 0));
        result.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(5, 4));
        result.ConfrontationTiePenaltyShootout.Should().BeTrue();
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_equal_return_play_score()
    {
        var tieFormat = TwoLegFormat();
        var act = () => CreateFlagged(tieFormat, new Score(1, 0), new Score(0, 0));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_non_decisive_shootout()
    {
        var tieFormat = TwoLegFormat();
        var act = () => MatchResult.CreateConfrontationTiePenaltyShootout(
            tieFormat,
            _entryA,
            _entryB,
            MatchStatus.Finished,
            new Score(1, 0),
            false,
            _entryB,
            _entryA,
            new Score(1, 0),
            false,
            new PenaltyShootoutScore(3, 3));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_level_first_leg_with_breaking_return_aggregate()
    {
        var tieFormat = TwoLegFormat();
        var act = () => CreateFlagged(tieFormat, new Score(1, 0), new Score(5, 0));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_another_breaking_return_score_for_same_first_leg()
    {
        var tieFormat = TwoLegFormat();
        CreateFlagged(tieFormat, new Score(1, 0), new Score(1, 0)).ConfrontationTiePenaltyShootout.Should().BeTrue();

        var act = () => CreateFlagged(tieFormat, new Score(1, 0), new Score(0, 3));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_equal_legs_without_shootout_rule_on_format()
    {
        var tieFormat = TwoLegFormat(withShootout: false);
        var act = () => CreateFlagged(tieFormat, new Score(0, 0), new Score(0, 0));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_when_return_leg_does_not_swap_entries()
    {
        var tieFormat = TwoLegFormat();
        var act = () => MatchResult.CreateConfrontationTiePenaltyShootout(
            tieFormat,
            _entryA,
            _entryB,
            MatchStatus.Finished,
            new Score(1, 0),
            false,
            _entryA,
            _entryB,
            new Score(0, 1),
            false,
            new PenaltyShootoutScore(5, 4));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_single_leg_format_without_aggregate()
    {
        var tieFormat = new TieFormat(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            penaltyShootoutRule: new PenaltyShootoutRule());
        var act = () => CreateFlagged(tieFormat, new Score(1, 0), new Score(0, 1));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_when_away_goals_break_level_aggregate()
    {
        var tieFormat = TwoLegFormat(awayGoals: true);
        var act = () => CreateFlagged(tieFormat, new Score(2, 1), new Score(1, 0));

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.InvalidResult);
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_accepts_same_scores_without_away_goals_rule()
    {
        var tieFormat = TwoLegFormat(awayGoals: false);
        var result = CreateFlagged(tieFormat, new Score(2, 1), new Score(1, 0));

        result.ConfrontationTiePenaltyShootout.Should().BeTrue();
    }

    [Fact]
    public void CreateConfrontationTiePenaltyShootout_rejects_when_first_leg_not_finished()
    {
        var tieFormat = TwoLegFormat();
        var act = () => MatchResult.CreateConfrontationTiePenaltyShootout(
            tieFormat,
            _entryA,
            _entryB,
            MatchStatus.Live,
            new Score(1, 0),
            false,
            _entryB,
            _entryA,
            new Score(1, 0),
            false,
            new PenaltyShootoutScore(5, 4));

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

    private MatchResult CreateFlagged(TieFormat tieFormat, Score firstScore, Score returnScore) =>
        MatchResult.CreateConfrontationTiePenaltyShootout(
            tieFormat,
            _entryA,
            _entryB,
            MatchStatus.Finished,
            firstScore,
            false,
            _entryB,
            _entryA,
            returnScore,
            false,
            new PenaltyShootoutScore(5, 4));
}
