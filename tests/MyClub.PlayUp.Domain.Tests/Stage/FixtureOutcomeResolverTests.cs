// -----------------------------------------------------------------------
// <copyright file="FixtureOutcomeResolverTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stage;

public sealed class FixtureOutcomeResolverTests
{
    private readonly FixtureId _fixtureId = FixtureId.New();
    private readonly EntryId _home = EntryId.New();
    private readonly EntryId _away = EntryId.New();

    private static TieFormat SingleLeg(bool withShootout = false) =>
        new(
            TieFormat.SingleLeg,
            aggregateScoring: false,
            penaltyShootoutRule: withShootout ? new PenaltyShootoutRule() : null);

    private static TieFormat TwoLeg(bool awayGoals = false, bool withShootout = false) =>
        new(
            TieFormat.TwoLegs,
            aggregateScoring: true,
            awayGoalsRule: awayGoals ? new AwayGoalsRule() : null,
            penaltyShootoutRule: withShootout ? new PenaltyShootoutRule() : null);

    [Fact]
    public void Resolve_single_leg_home_wins()
    {
        var outcome = FixtureOutcomeResolver.Resolve(SingleLeg(), OneLeg(new Score(2, 1)));

        outcome.WinnerEntryId.Should().Be(_home);
        outcome.LoserEntryId.Should().Be(_away);
    }

    [Fact]
    public void Resolve_single_leg_away_wins()
    {
        var outcome = FixtureOutcomeResolver.Resolve(SingleLeg(), OneLeg(new Score(1, 2)));

        outcome.WinnerEntryId.Should().Be(_away);
        outcome.LoserEntryId.Should().Be(_home);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 2)]
    public void Resolve_single_leg_draw_without_shootout_is_undecided(int homeGoals, int awayGoals)
    {
        var act = () => FixtureOutcomeResolver.Resolve(SingleLeg(), OneLeg(new Score(homeGoals, awayGoals)));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeUndecided);
    }

    [Fact]
    public void Resolve_single_leg_draw_with_shootout_when_rule_allows()
    {
        var outcome = FixtureOutcomeResolver.Resolve(
            SingleLeg(withShootout: true),
            OneLeg(new Score(1, 1), shootout: new PenaltyShootoutScore(5, 4)));

        outcome.WinnerEntryId.Should().Be(_home);
    }

    [Fact]
    public void Resolve_single_leg_tab_present_without_rule_is_invalid()
    {
        var act = () => FixtureOutcomeResolver.Resolve(
            SingleLeg(withShootout: false),
            OneLeg(new Score(1, 1), shootout: new PenaltyShootoutScore(5, 4)));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_single_leg_tab_without_extra_time_is_allowed()
    {
        var outcome = FixtureOutcomeResolver.Resolve(
            SingleLeg(withShootout: true),
            OneLeg(new Score(0, 0), extraTimePlayed: false, shootout: new PenaltyShootoutScore(4, 3)));

        outcome.WinnerEntryId.Should().Be(_home);
    }

    [Theory]
    [InlineData(MatchStatus.Scheduled)]
    [InlineData(MatchStatus.Live)]
    [InlineData(MatchStatus.Postponed)]
    [InlineData(MatchStatus.Cancelled)]
    public void Resolve_rejects_when_not_finished(MatchStatus status)
    {
        var snapshot = new FixtureConfrontationSnapshot(
            _fixtureId,
            [
                new FixtureLegSnapshot(1, MatchId.New(), _home, _away, status, score: null, extraTimePlayed: false)
            ]);

        var act = () => FixtureOutcomeResolver.Resolve(SingleLeg(), snapshot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeNotFinished);
    }

    [Fact]
    public void Resolve_rejects_finished_without_score()
    {
        var snapshot = new FixtureConfrontationSnapshot(
            _fixtureId,
            [
                new FixtureLegSnapshot(1, MatchId.New(), _home, _away, MatchStatus.Finished, score: null, false)
            ]);

        var act = () => FixtureOutcomeResolver.Resolve(SingleLeg(), snapshot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_rejects_same_home_and_away()
    {
        var same = EntryId.New();
        var snapshot = new FixtureConfrontationSnapshot(
            _fixtureId,
            [
                new FixtureLegSnapshot(1, MatchId.New(), same, same, MatchStatus.Finished, new Score(1, 0), false)
            ]);

        var act = () => FixtureOutcomeResolver.Resolve(SingleLeg(), snapshot);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_two_leg_aggregate_home_wins()
    {
        var outcome = FixtureOutcomeResolver.Resolve(
            TwoLeg(),
            Confrontation(
                Leg(1, _home, _away, new Score(2, 0)),
                Leg(2, _away, _home, new Score(1, 0))));

        outcome.WinnerEntryId.Should().Be(_home);
        outcome.LoserEntryId.Should().Be(_away);
    }

    [Fact]
    public void Resolve_two_leg_aggregate_with_inverted_homes()
    {
        var outcome = FixtureOutcomeResolver.Resolve(
            TwoLeg(),
            Confrontation(
                Leg(2, _away, _home, new Score(1, 0)),
                Leg(1, _home, _away, new Score(2, 0))));

        outcome.WinnerEntryId.Should().Be(_home);
    }

    [Fact]
    public void Resolve_two_leg_away_goals_decides_when_aggregate_tied()
    {
        // Leg1 A 2-1 B; Leg2 B 1-0 A → aggregate 2-2; B has 1 away goal, A has 0 → B wins
        var outcome = FixtureOutcomeResolver.Resolve(
            TwoLeg(awayGoals: true),
            Confrontation(
                Leg(1, _home, _away, new Score(2, 1)),
                Leg(2, _away, _home, new Score(1, 0))));

        outcome.WinnerEntryId.Should().Be(_away);
    }

    [Fact]
    public void Resolve_two_leg_aggregate_tied_without_ag_uses_shootout_on_last_leg()
    {
        var outcome = FixtureOutcomeResolver.Resolve(
            TwoLeg(withShootout: true),
            Confrontation(
                Leg(1, _home, _away, new Score(1, 0)),
                Leg(2, _away, _home, new Score(1, 0), extraTimePlayed: true, shootout: new PenaltyShootoutScore(5, 4))));

        outcome.WinnerEntryId.Should().Be(_away);
    }

    [Fact]
    public void Resolve_two_leg_aggregate_tied_undecided_without_shootout()
    {
        var act = () => FixtureOutcomeResolver.Resolve(
            TwoLeg(),
            Confrontation(
                Leg(1, _home, _away, new Score(1, 1)),
                Leg(2, _away, _home, new Score(0, 0))));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeUndecided);
    }

    [Fact]
    public void Resolve_rejects_et_on_non_final_leg()
    {
        var act = () => FixtureOutcomeResolver.Resolve(
            TwoLeg(),
            Confrontation(
                Leg(1, _home, _away, new Score(1, 1), extraTimePlayed: true),
                Leg(2, _away, _home, new Score(0, 0))));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_rejects_tab_on_non_final_leg()
    {
        var act = () => FixtureOutcomeResolver.Resolve(
            TwoLeg(withShootout: true),
            Confrontation(
                Leg(1, _home, _away, new Score(1, 1), shootout: new PenaltyShootoutScore(4, 3)),
                Leg(2, _away, _home, new Score(0, 0))));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_rejects_wrong_leg_cardinality()
    {
        var act = () => FixtureOutcomeResolver.Resolve(TwoLeg(), OneLeg(new Score(1, 0)));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_rejects_missing_leg_index()
    {
        var act = () => FixtureOutcomeResolver.Resolve(
            TwoLeg(),
            Confrontation(
                Leg(1, _home, _away, new Score(1, 0)),
                Leg(3, _away, _home, new Score(0, 0))));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_rejects_more_than_two_entries()
    {
        var third = EntryId.New();
        var act = () => FixtureOutcomeResolver.Resolve(
            TwoLeg(),
            Confrontation(
                Leg(1, _home, _away, new Score(1, 0)),
                Leg(2, _away, third, new Score(0, 0))));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeInvalid);
    }

    [Fact]
    public void Resolve_rejects_null_arguments()
    {
        ((Action)(() => FixtureOutcomeResolver.Resolve(null!, OneLeg(new Score(1, 0)))))
            .Should().Throw<ArgumentNullException>();
        ((Action)(() => FixtureOutcomeResolver.Resolve(SingleLeg(), null!)))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Leg_snapshot_carries_extra_time_played()
    {
        typeof(FixtureLegSnapshot).GetProperty("ExtraTimePlayed").Should().NotBeNull();
        typeof(FixtureConfrontationSnapshot).GetProperty("Legs").Should().NotBeNull();
    }

    private static FixtureLegSnapshot Leg(
        int index,
        EntryId home,
        EntryId away,
        Score score,
        bool extraTimePlayed = false,
        PenaltyShootoutScore? shootout = null) =>
        new(index, MatchId.New(), home, away, MatchStatus.Finished, score, extraTimePlayed, shootout);

    private FixtureConfrontationSnapshot OneLeg(
        Score score,
        bool extraTimePlayed = false,
        PenaltyShootoutScore? shootout = null) =>
        Confrontation(Leg(1, _home, _away, score, extraTimePlayed, shootout));

    private FixtureConfrontationSnapshot Confrontation(params FixtureLegSnapshot[] legs) =>
        new(_fixtureId, legs);
}
