// -----------------------------------------------------------------------
// <copyright file="ApplyProgressionOutcomeTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stage;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Match;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Stage.Events;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Application.Tests.Stage;

public sealed class ApplyProgressionOutcomeTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 20, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_self_stage_winner_sets_home_entry()
    {
        var ctx = CreateSelfStageContext(homeGoals: 2, awayGoals: 1, withLoserPath: false);

        var results = ApplyProgressionOutcome.Execute(
            ctx.Source,
            ctx.FixtureId,
            ctx.Match,
            [ctx.Source],
            _clock);

        results.Should().ContainSingle();
        results[0].EntryId.Should().Be(ctx.Home);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().Be(ctx.Home);
    }

    [Fact]
    public void Execute_cross_stage_sets_destination_slot()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A", "QF1-B"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A", "SF1-B"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, homeGoals: 1, awayGoals: 0);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(destination.Id, "SF1-A"))
            ]),
            _clock);

        var results = ApplyProgressionOutcome.Execute(
            source,
            fixtureId,
            match,
            [source, destination],
            _clock);

        results.Should().ContainSingle();
        destination.FindSlot("SF1-A")!.EntryId.Should().Be(home);
        source.FindSlot("QF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_winner_and_loser_paths_resolve_both_destinations()
    {
        var ctx = CreateSelfStageContext(homeGoals: 3, awayGoals: 1, withLoserPath: true);

        var results = ApplyProgressionOutcome.Execute(
            ctx.Source,
            ctx.FixtureId,
            ctx.Match,
            [ctx.Source],
            _clock);

        results.Should().HaveCount(2);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().Be(ctx.Home);
        ctx.Source.FindSlot("Consolante-1")!.EntryId.Should().Be(ctx.Away);
    }

    [Fact]
    public void Execute_rejects_fixture_without_match()
    {
        var stage = CreateKnockoutStage(CompetitionId.New(), "QF", ["SF1-A"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A");
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);
        var match = Match.Create(stage.CompetitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, match, [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_rejects_fixture_with_multiple_matches()
    {
        var stage = CreateKnockoutStage(CompetitionId.New(), "QF", ["SF1-A"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A");
        var home = EntryId.New();
        var away = EntryId.New();
        var first = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        var second = Match.Create(stage.CompetitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);
        stage.AttachMatch(fixture.Id, first.Id, _clock);
        stage.AttachMatch(fixture.Id, second.Id, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);
        Finish(first, homeGoals: 2, awayGoals: 1);

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, first, [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_rejects_match_not_bound_to_fixture()
    {
        var ctx = CreateSelfStageContext(homeGoals: 2, awayGoals: 1, withLoserPath: false);
        var other = Match.Create(ctx.Source.CompetitionId, ctx.Source.Id, EntryId.New(), EntryId.New(), _clock);
        Finish(other, 1, 0);

        var act = () => ApplyProgressionOutcome.Execute(
            ctx.Source,
            ctx.FixtureId,
            other,
            [ctx.Source],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_draw_does_not_mutate()
    {
        var ctx = CreateSelfStageContext(homeGoals: 2, awayGoals: 2, withLoserPath: true);

        var act = () => ApplyProgressionOutcome.Execute(
            ctx.Source,
            ctx.FixtureId,
            ctx.Match,
            [ctx.Source],
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeUndecided);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        ctx.Source.FindSlot("Consolante-1")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_rejects_when_match_not_finished()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateKnockoutStage(competitionId, "QF", ["SF1-A", "Consolante-1"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, match, [stage], _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeNotFinished);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_rejects_missing_destination_stage_without_mutation()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A"]);
        var missingDestinationId = StageId.New();
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, 2, 0);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(missingDestinationId, "SF1-A"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(source, fixtureId, match, [source], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
        source.FindSlot("QF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_rejects_missing_destination_slot_without_mutation()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, 2, 0);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(destination.Id, "Missing-Slot"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(
            source,
            fixtureId,
            match,
            [source, destination],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DanglingFeedTarget);
        destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_rejects_match_stage_id_mismatch()
    {
        var stage = CreateKnockoutStage(CompetitionId.New(), "QF", ["SF1-A"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(stage.CompetitionId, StageId.New(), home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, _clock);
        Finish(match, 2, 1);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, match, [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_rejects_match_competition_id_mismatch()
    {
        var stage = CreateKnockoutStage(CompetitionId.New(), "QF", ["SF1-A"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(CompetitionId.New(), stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, _clock);
        Finish(match, 2, 1);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, match, [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_direct_conflict_bubbles_domain_error()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A"]);
        var directEntry = EntryId.New();
        destination.AssignEntryToSlot("SF1-A", directEntry, _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, 2, 1);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(destination.Id, "SF1-A"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(
            source,
            fixtureId,
            match,
            [source, destination],
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.SlotFeedConflict);
        destination.FindSlot("SF1-A")!.EntryId.Should().Be(directEntry);
    }

    [Fact]
    public void Execute_preflight_second_path_missing_slot_mutates_nothing()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, 2, 0);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(destination.Id, "SF1-A")),
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Loser,
                    new ProgressionDestination(destination.Id, "Missing-Slot"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(
            source,
            fixtureId,
            match,
            [source, destination],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DanglingFeedTarget);
        destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_idempotent_second_call_is_noop_for_events()
    {
        var ctx = CreateSelfStageContext(homeGoals: 2, awayGoals: 1, withLoserPath: false);
        ApplyProgressionOutcome.Execute(ctx.Source, ctx.FixtureId, ctx.Match, [ctx.Source], _clock);
        ctx.Source.ClearDomainEvents();

        ApplyProgressionOutcome.Execute(ctx.Source, ctx.FixtureId, ctx.Match, [ctx.Source], _clock);

        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().Be(ctx.Home);
        ctx.Source.DomainEvents.Should().NotContain(e => e is StageSlotOccupantChanged);
    }

    [Fact]
    public void Execute_replaces_existing_resolved_occupant()
    {
        var ctx = CreateSelfStageContext(homeGoals: 2, awayGoals: 1, withLoserPath: false);
        var previous = EntryId.New();
        ctx.Source.ApplyResolvedEntry("SF1-A", previous, _clock);

        ApplyProgressionOutcome.Execute(ctx.Source, ctx.FixtureId, ctx.Match, [ctx.Source], _clock);

        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().Be(ctx.Home);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().NotBe(previous);
    }

    [Fact]
    public void Execute_returns_empty_when_no_paths_without_resolving_outcome()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateKnockoutStage(competitionId, "QF", ["SF1-A"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);
        stage.AttachMatch(fixture.Id, match.Id, _clock);

        var results = ApplyProgressionOutcome.Execute(stage, fixture.Id, match, [stage], _clock);

        results.Should().BeEmpty();
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        match.Status.Should().Be(MatchStatus.Scheduled);
    }

    [Fact]
    public void Execute_rejects_source_stage_not_in_competition_list()
    {
        var ctx = CreateSelfStageContext(homeGoals: 1, awayGoals: 0, withLoserPath: false);
        var other = CreateKnockoutStage(CompetitionId.New(), "Other", ["X"]);

        var act = () => ApplyProgressionOutcome.Execute(
            ctx.Source,
            ctx.FixtureId,
            ctx.Match,
            [other],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
    }

    [Fact]
    public void Assemble_maps_match_fields_without_sports_decisions()
    {
        var competitionId = CompetitionId.New();
        var stageId = StageId.New();
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(competitionId, stageId, home, away, _clock);
        var fixtureId = FixtureId.New();

        var snapshot = FixtureOutcomeSnapshotAssembler.Assemble(fixtureId, match);

        snapshot.FixtureId.Should().Be(fixtureId);
        snapshot.MatchId.Should().Be(match.Id);
        snapshot.HomeEntryId.Should().Be(home);
        snapshot.AwayEntryId.Should().Be(away);
        snapshot.Status.Should().Be(MatchStatus.Scheduled);
        snapshot.Score.Should().BeNull();
    }

    private SelfStageContext CreateSelfStageContext(int homeGoals, int awayGoals, bool withLoserPath)
    {
        var competitionId = CompetitionId.New();
        var slotKeys = withLoserPath ? new[] { "SF1-A", "Consolante-1" } : ["SF1-A"];
        var source = CreateKnockoutStage(competitionId, "Knockout", slotKeys);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, homeGoals, awayGoals);

        var paths = new List<ProgressionPath>
        {
            new(
                fixtureId,
                ProgressionOutcome.Winner,
                new ProgressionDestination(source.Id, "SF1-A"))
        };
        if (withLoserPath)
        {
            paths.Add(
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Loser,
                    new ProgressionDestination(source.Id, "Consolante-1")));
        }

        source.ReplaceProgressionRules(new ProgressionRules(paths), _clock);
        return new SelfStageContext(source, fixtureId, match, home, away);
    }

    private StageAggregate CreateKnockoutStage(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = StageAggregate.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key, _clock);
        }

        return stage;
    }

    private (FixtureId FixtureId, Match Match) AttachFinishedMatch(
        StageAggregate stage,
        EntryId home,
        EntryId away,
        int homeGoals,
        int awayGoals)
    {
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, _clock);
        Finish(match, homeGoals, awayGoals);
        return (fixture.Id, match);
    }

    private void Finish(Match match, int homeGoals, int awayGoals)
    {
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
    }

    private sealed record SelfStageContext(
        StageAggregate Source,
        FixtureId FixtureId,
        Match Match,
        EntryId Home,
        EntryId Away);
}
