// -----------------------------------------------------------------------
// <copyright file="FixtureOutcomeResolverTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Stage;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stage;

public sealed class FixtureOutcomeResolverTests
{
    private readonly FixtureId _fixtureId = FixtureId.New();
    private readonly MatchId _matchId = MatchId.New();
    private readonly EntryId _home = EntryId.New();
    private readonly EntryId _away = EntryId.New();

    [Fact]
    public void Resolve_home_wins_two_one()
    {
        var snapshot = Finished(new Score(2, 1));

        var outcome = FixtureOutcomeResolver.Resolve(snapshot);

        outcome.WinnerEntryId.Should().Be(_home);
        outcome.LoserEntryId.Should().Be(_away);
    }

    [Fact]
    public void Resolve_away_wins_one_two()
    {
        var snapshot = Finished(new Score(1, 2));

        var outcome = FixtureOutcomeResolver.Resolve(snapshot);

        outcome.WinnerEntryId.Should().Be(_away);
        outcome.LoserEntryId.Should().Be(_home);
    }

    [Fact]
    public void Resolve_forfeit_score_uses_same_derivation()
    {
        var snapshot = Finished(new Score(3, 0));

        var outcome = FixtureOutcomeResolver.Resolve(snapshot);

        outcome.WinnerEntryId.Should().Be(_home);
        outcome.LoserEntryId.Should().Be(_away);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    public void Resolve_draw_is_undecided(int homeGoals, int awayGoals)
    {
        var snapshot = Finished(new Score(homeGoals, awayGoals));

        var act = () => FixtureOutcomeResolver.Resolve(snapshot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeUndecided);
    }

    [Theory]
    [InlineData(MatchStatus.Scheduled)]
    [InlineData(MatchStatus.Live)]
    [InlineData(MatchStatus.Postponed)]
    [InlineData(MatchStatus.Cancelled)]
    public void Resolve_rejects_when_not_finished(MatchStatus status)
    {
        var snapshot = new FixtureOutcomeSnapshot(_fixtureId, _matchId, _home, _away, status, score: null);

        var act = () => FixtureOutcomeResolver.Resolve(snapshot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeNotFinished);
    }

    [Fact]
    public void Resolve_rejects_finished_without_score()
    {
        var snapshot = new FixtureOutcomeSnapshot(
            _fixtureId,
            _matchId,
            _home,
            _away,
            MatchStatus.Finished,
            score: null);

        var act = () => FixtureOutcomeResolver.Resolve(snapshot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_rejects_same_home_and_away()
    {
        var same = EntryId.New();
        var snapshot = new FixtureOutcomeSnapshot(
            _fixtureId,
            _matchId,
            same,
            same,
            MatchStatus.Finished,
            new Score(1, 0));

        var act = () => FixtureOutcomeResolver.Resolve(snapshot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_rejects_null_snapshot()
    {
        var act = () => FixtureOutcomeResolver.Resolve(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Snapshot_ctor_allows_same_home_and_away_without_throwing()
    {
        var same = EntryId.New();

        var act = () => new FixtureOutcomeSnapshot(
            _fixtureId,
            _matchId,
            same,
            same,
            MatchStatus.Finished,
            new Score(1, 0));

        act.Should().NotThrow();
    }

    [Fact]
    public void Resolve_draw_with_home_shootout_win()
    {
        var snapshot = Finished(new Score(2, 2), new PenaltyShootoutScore(4, 3));

        var outcome = FixtureOutcomeResolver.Resolve(snapshot);

        outcome.WinnerEntryId.Should().Be(_home);
        outcome.LoserEntryId.Should().Be(_away);
    }

    [Fact]
    public void Resolve_draw_with_away_shootout_win()
    {
        var snapshot = Finished(new Score(0, 0), new PenaltyShootoutScore(4, 5));

        var outcome = FixtureOutcomeResolver.Resolve(snapshot);

        outcome.WinnerEntryId.Should().Be(_away);
        outcome.LoserEntryId.Should().Be(_home);
    }

    [Fact]
    public void Resolve_rejects_equal_shootout_as_invalid()
    {
        var snapshot = Finished(new Score(1, 1), new PenaltyShootoutScore(3, 3));

        var act = () => FixtureOutcomeResolver.Resolve(snapshot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_outcome_identical_regardless_of_match_extra_time_flag()
    {
        // ExtraTimePlayed lives on MatchResult only — snapshot has no ET field; same Score/TAB ⇒ same Winner.
        var withoutEt = new MatchResult(ResultType.Played, new Score(2, 1), extraTimePlayed: false);
        var withEt = new MatchResult(ResultType.Played, new Score(2, 1), extraTimePlayed: true);

        var outcomeWithoutEt = FixtureOutcomeResolver.Resolve(Finished(withoutEt.Score, withoutEt.PenaltyShootoutScore));
        var outcomeWithEt = FixtureOutcomeResolver.Resolve(Finished(withEt.Score, withEt.PenaltyShootoutScore));

        outcomeWithoutEt.Should().Be(outcomeWithEt);
        outcomeWithEt.WinnerEntryId.Should().Be(_home);
    }

    [Fact]
    public void Snapshot_does_not_carry_extra_time_played()
    {
        typeof(FixtureOutcomeSnapshot).GetProperty("ExtraTimePlayed").Should().BeNull();
        typeof(FixtureOutcomeSnapshot).GetProperty("PenaltyShootoutScore").Should().NotBeNull();
    }

    private FixtureOutcomeSnapshot Finished(Score score, PenaltyShootoutScore? shootout = null) =>
        new(_fixtureId, _matchId, _home, _away, MatchStatus.Finished, score, shootout);
}
