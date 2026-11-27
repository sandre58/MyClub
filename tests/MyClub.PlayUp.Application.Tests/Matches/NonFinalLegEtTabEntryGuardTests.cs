// -----------------------------------------------------------------------
// <copyright file="NonFinalLegEtTabEntryGuardTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Matches;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Matches;

public sealed class NonFinalLegEtTabEntryGuardTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 20, 0, 0, TimeSpan.Zero));

    [Fact]
    public void EnsureAllowed_ignores_unattached_match_with_et()
    {
        var stage = CreateRunningChampionshipStage();
        var match = Match.Create(stage.CompetitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);
        match.Start(_clock);
        var result = new MatchResult(ResultType.Played, new Score(1, 1), extraTimePlayed: true);

        var act = () => NonFinalLegEtTabEntryGuard.EnsureAllowed(stage, match.Id, result);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAllowed_rejects_extra_time_on_first_leg_of_two()
    {
        var (stage, first, _) = CreateTwoLegCup();
        first.Start(_clock);
        var result = new MatchResult(ResultType.Played, new Score(1, 1), extraTimePlayed: true);

        var act = () => NonFinalLegEtTabEntryGuard.EnsureAllowed(stage, first.Id, result);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.ExtraTimeOrShootoutOnNonFinalLeg);
    }

    [Fact]
    public void EnsureAllowed_rejects_shootout_on_first_leg_of_two()
    {
        var (stage, first, _) = CreateTwoLegCup();
        first.Start(_clock);
        var result = new MatchResult(
            ResultType.Played,
            new Score(1, 1),
            penaltyShootoutScore: new PenaltyShootoutScore(4, 3));

        var act = () => NonFinalLegEtTabEntryGuard.EnsureAllowed(stage, first.Id, result);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(MatchErrorCodes.ExtraTimeOrShootoutOnNonFinalLeg);
    }

    [Fact]
    public void EnsureAllowed_allows_et_on_final_leg_of_two()
    {
        var (stage, _, second) = CreateTwoLegCup();
        second.Start(_clock);
        var result = new MatchResult(ResultType.Played, new Score(0, 0), extraTimePlayed: true);

        var act = () => NonFinalLegEtTabEntryGuard.EnsureAllowed(stage, second.Id, result);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureAllowed_allows_et_on_single_leg_fixture()
    {
        var (stage, match) = CreateSingleLegCup();
        match.Start(_clock);
        var result = new MatchResult(
            ResultType.Played,
            new Score(1, 1),
            extraTimePlayed: true,
            penaltyShootoutScore: new PenaltyShootoutScore(5, 4));

        var act = () => NonFinalLegEtTabEntryGuard.EnsureAllowed(stage, match.Id, result);

        act.Should().NotThrow();
    }

    private (Stage Stage, Match First, Match Second) CreateTwoLegCup()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "A", "B", "P1");
        var home = EntryId.New();
        var away = EntryId.New();
        var first = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        var second = Match.Create(stage.CompetitionId, stage.Id, away, home, _clock);
        stage.AttachMatch(fixture.Id, first.Id, legIndex: 1, _clock);
        stage.AttachMatch(fixture.Id, second.Id, legIndex: 2, _clock);
        return (stage, first, second);
    }

    private (Stage Stage, Match Match) CreateSingleLegCup()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("Final", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("A");
        stage.AddSlot("B");
        stage.ReplaceBracketPairs([new BracketPair("P1", "A", "B")]);
        var fixture = stage.AddFixture(round.Id, _clock, "A", "B", "P1");
        var match = Match.Create(stage.CompetitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        return (stage, match);
    }

    private Stage CreateRunningChampionshipStage()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("League"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        return stage;
    }
}
