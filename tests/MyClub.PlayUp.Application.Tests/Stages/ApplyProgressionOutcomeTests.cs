// -----------------------------------------------------------------------
// <copyright file="ApplyProgressionOutcomeTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

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
            [ctx.Match],
            [ctx.Source],
            _clock);

        results.Should().ContainSingle();
        results[0].EntryId.Should().Be(ctx.Home);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().Be(ctx.Home);
    }

    [Fact]
    public void Execute_cross_stage_adds_destination_population()
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
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);

        var results = ApplyProgressionOutcome.Execute(
            source,
            fixtureId,
            [match],
            [source, destination],
            _clock);

        results.Should().ContainSingle();
        results[0].TargetsPopulation.Should().BeTrue();
        destination.CompositionEntries.Select(e => e.EntryId).Should().Equal(home);
        destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        source.FindSlot("QF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_winner_and_loser_paths_resolve_both_destinations()
    {
        var ctx = CreateSelfStageContext(homeGoals: 3, awayGoals: 1, withLoserPath: true);

        var results = ApplyProgressionOutcome.Execute(
            ctx.Source,
            ctx.FixtureId,
            [ctx.Match],
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

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, [match], [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_two_leg_fixture_resolves_aggregate_winner()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("QF"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        stage.AddSlot("SF1-A");
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A");
        var home = EntryId.New();
        var away = EntryId.New();
        var first = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        var second = Match.Create(stage.CompetitionId, stage.Id, away, home, _clock);
        stage.AttachMatch(fixture.Id, first.Id, legIndex: 1, _clock);
        stage.AttachMatch(fixture.Id, second.Id, legIndex: 2, _clock);
        Finish(first, homeGoals: 1, awayGoals: 0);
        Finish(second, homeGoals: 0, awayGoals: 2);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var results = ApplyProgressionOutcome.Execute(
            stage,
            fixture.Id,
            [first, second],
            [stage],
            _clock);

        results.Should().ContainSingle();
        results[0].EntryId.Should().Be(home);
        stage.FindSlot("SF1-A")!.EntryId.Should().Be(home);
    }

    [Fact]
    public void Execute_rejects_attachments_count_mismatch_vs_tie_format()
    {
        var stage = CreateKnockoutStage(CompetitionId.New(), "QF", ["SF1-A"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock, "SF1-A");
        var home = EntryId.New();
        var away = EntryId.New();
        var first = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        var second = Match.Create(stage.CompetitionId, stage.Id, away, home, _clock);
        stage.AttachMatch(fixture.Id, first.Id, legIndex: 1, _clock);
        stage.AttachMatch(fixture.Id, second.Id, legIndex: 2, _clock);
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
        Finish(second, homeGoals: 0, awayGoals: 1);

        var act = () => ApplyProgressionOutcome.Execute(
            stage,
            fixture.Id,
            [first, second],
            [stage],
            _clock);

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
            [other],
            [ctx.Source],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_tied_match_score_does_not_mutate()
    {
        var ctx = CreateSelfStageContext(homeGoals: 2, awayGoals: 2, withLoserPath: true);

        var act = () => ApplyProgressionOutcome.Execute(
            ctx.Source,
            ctx.FixtureId,
            [ctx.Match],
            [ctx.Source],
            _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.FixtureOutcomeUndecided);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        ctx.Source.FindSlot("Consolante-1")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_tied_score_with_shootout_sets_winner()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(
            competitionId,
            new StageName("Knockout"),
            SampleRegulations.Standard(),
            _clock);
        source.AddRound(
            "R1",
            new TieFormat(TieFormat.SingleLeg, false, penaltyShootoutRule: new PenaltyShootoutRule()),
            _clock);
        source.AddSlot("SF1-A");
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock);
        var match = Match.Create(source.CompetitionId, source.Id, home, away, _clock);
        source.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(
            new MatchResult(
                ResultType.Played,
                new Score(1, 1),
                extraTimePlayed: false,
                new PenaltyShootoutScore(5, 4)),
            _clock);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(source.Id, "SF1-A"))
            ]),
            _clock);

        var results = ApplyProgressionOutcome.Execute(source, fixture.Id, [match], [source], _clock);

        results.Should().ContainSingle();
        results[0].EntryId.Should().Be(home);
        source.FindSlot("SF1-A")!.EntryId.Should().Be(home);
    }

    [Fact]
    public void Assemble_maps_shootout_from_finished_match()
    {
        var stage = CreateKnockoutStage(CompetitionId.New(), "Knockout", ["SF1-A"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        match.Start(_clock);
        match.Finish(
            new MatchResult(
                ResultType.Played,
                new Score(2, 2),
                extraTimePlayed: true,
                new PenaltyShootoutScore(4, 3)),
            _clock);

        var snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, [match]);

        snapshot.FixtureId.Should().Be(fixture.Id);
        snapshot.Legs.Should().ContainSingle();
        var leg = snapshot.Legs[0];
        leg.Score.Should().Be(new Score(2, 2));
        leg.ExtraTimePlayed.Should().BeTrue();
        leg.PenaltyShootoutScore.Should().Be(new PenaltyShootoutScore(4, 3));
    }

    [Fact]
    public void Execute_applies_when_destination_suspended()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A", "QF1-B"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, homeGoals: 1, awayGoals: 0);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);
        destination.Prepare(_clock);
        destination.Start(_clock);
        destination.Suspend(_clock);

        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        destination.Status.Should().Be(StageStatus.Suspended);
        destination.CompositionEntries.Select(e => e.EntryId).Should().Equal(home);
        destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        destination.Draws.Should().BeEmpty();
    }

    [Fact]
    public void Execute_rejects_when_destination_completed()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A", "QF1-B"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, homeGoals: 1, awayGoals: 0);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);
        destination.Prepare(_clock);
        destination.Start(_clock);
        destination.Complete(_clock);

        var act = () => ApplyProgressionOutcome.Execute(
            source,
            fixtureId,
            [match],
            [source, destination],
            _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        destination.CompositionEntries.Should().BeEmpty();
        destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
        destination.Status.Should().Be(StageStatus.Completed);
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
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, [match], [stage], _clock);

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
                    ProgressionDestination.ForPopulation(missingDestinationId))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
        source.FindSlot("QF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void ReplaceProgressionRules_rejects_cross_stage_place()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A"]);
        var fixture = source.AddFixture(source.Rounds[0].Id, _clock);

        var act = () => source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(destination.Id, "SF1-A"))
            ]),
            _clock);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Code.Should().Be(RulesErrorCodes.ProgressionRulesInvalid);
        ex.Message.Should().Contain("form-owning");
    }

    [Fact]
    public void Execute_rejects_match_stage_id_mismatch()
    {
        var stage = CreateKnockoutStage(CompetitionId.New(), "QF", ["SF1-A"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(stage.CompetitionId, StageId.New(), home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
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

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, [match], [stage], _clock);

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
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
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

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, [match], [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.FixtureInvalid);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_population_succeeds_alongside_destination_direct_slot_assignment()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A"]);
        var directEntry = EntryId.New();
        destination.AssignEntryToSlot("SF1-A", directEntry);
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, 2, 1);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id))
            ]),
            _clock);

        ApplyProgressionOutcome.Execute(source, fixtureId, [match], [source, destination], _clock);

        destination.CompositionEntries.Select(e => e.EntryId).Should().Equal(home);
        destination.FindSlot("SF1-A")!.EntryId.Should().Be(directEntry);
        destination.DirectAssignments.Should().ContainSingle();
    }

    [Fact]
    public void Execute_preflight_second_path_missing_stage_mutates_nothing()
    {
        var competitionId = CompetitionId.New();
        var source = CreateKnockoutStage(competitionId, "QF", ["QF1-A"]);
        var destination = CreateKnockoutStage(competitionId, "SF", ["SF1-A"]);
        var missingLoserDestinationId = StageId.New();
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(source, home, away, 2, 0);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(destination.Id)),
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Loser,
                    ProgressionDestination.ForPopulation(missingLoserDestinationId))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(
            source,
            fixtureId,
            [match],
            [source, destination],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
        destination.CompositionEntries.Should().BeEmpty();
        destination.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Execute_idempotent_second_call_is_noop_for_events()
    {
        var ctx = CreateSelfStageContext(homeGoals: 2, awayGoals: 1, withLoserPath: false);
        ApplyProgressionOutcome.Execute(ctx.Source, ctx.FixtureId, [ctx.Match], [ctx.Source], _clock);
        ctx.Source.ClearDomainEvents();

        ApplyProgressionOutcome.Execute(ctx.Source, ctx.FixtureId, [ctx.Match], [ctx.Source], _clock);

        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().Be(ctx.Home);
        ctx.Source.DomainEvents.Should().NotContain(e => e is StageSlotOccupantChanged);
    }

    [Fact]
    public void Execute_when_slot_occupied_by_different_entry_rejects_without_mutation()
    {
        var ctx = CreateSelfStageContext(homeGoals: 2, awayGoals: 1, withLoserPath: false);
        var previous = EntryId.New();
        ctx.Source.ApplyResolvedEntry("SF1-A", previous, _clock);
        ctx.Source.ClearDomainEvents();

        var act = () => ApplyProgressionOutcome.Execute(
            ctx.Source,
            ctx.FixtureId,
            [ctx.Match],
            [ctx.Source],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SlotOccupancyConflict);
        ctx.Source.FindSlot("SF1-A")!.EntryId.Should().Be(previous);
        ctx.Source.DomainEvents.Should().NotContain(e => e is StageSlotOccupantChanged);
    }

    [Fact]
    public void Execute_returns_empty_when_no_paths_without_resolving_outcome()
    {
        var competitionId = CompetitionId.New();
        var stage = CreateKnockoutStage(competitionId, "QF", ["SF1-A"]);
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competitionId, stage.Id, EntryId.New(), EntryId.New(), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var results = ApplyProgressionOutcome.Execute(stage, fixture.Id, [match], [stage], _clock);

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
            [ctx.Match],
            [other],
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
    }

    [Fact]
    public void Execute_applies_one_leg_when_round_has_no_tie_format()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("QF"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("R1", tieFormat: null, _clock);
        stage.AddSlot("SF1-A");
        var home = EntryId.New();
        var away = EntryId.New();
        var (fixtureId, match) = AttachFinishedMatch(stage, home, away, homeGoals: 1, awayGoals: 0);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixtureId,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var instructions = ApplyProgressionOutcome.Execute(stage, fixtureId, [match], [stage], _clock);

        instructions.Should().ContainSingle();
        instructions[0].EntryId.Should().Be(home);
        stage.FindSlot("SF1-A")!.EntryId.Should().Be(home);
    }

    [Fact]
    public void Execute_rejects_when_fixture_is_under_matchday()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("League"),
            SampleRegulations.Standard(),
            _clock);
        var matchday = stage.AddMatchday(1, _clock);
        stage.AddSlot("SF1-A");
        var fixture = stage.AddFixture(matchday.Id, _clock);
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        Finish(match, homeGoals: 1, awayGoals: 0);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        var act = () => ApplyProgressionOutcome.Execute(stage, fixture.Id, [match], [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.TieFormatRequired);
        stage.FindSlot("SF1-A")!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Assemble_maps_match_fields_without_sports_decisions()
    {
        var stage = CreateKnockoutStage(CompetitionId.New(), "Knockout", ["SF1-A"]);
        var home = EntryId.New();
        var away = EntryId.New();
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var snapshot = FixtureConfrontationSnapshotAssembler.Assemble(fixture, [match]);

        snapshot.FixtureId.Should().Be(fixture.Id);
        snapshot.Legs.Should().ContainSingle();
        var leg = snapshot.Legs[0];
        leg.LegIndex.Should().Be(1);
        leg.MatchId.Should().Be(match.Id);
        leg.HomeEntryId.Should().Be(home);
        leg.AwayEntryId.Should().Be(away);
        leg.Status.Should().Be(MatchStatus.Scheduled);
        leg.Score.Should().BeNull();
        leg.ExtraTimePlayed.Should().BeFalse();
        leg.PenaltyShootoutScore.Should().BeNull();
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

    private Stage CreateKnockoutStage(CompetitionId competitionId, string name, string[] slotKeys)
    {
        var stage = Stage.Create(competitionId, new StageName(name), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        foreach (var key in slotKeys)
        {
            stage.AddSlot(key);
        }

        return stage;
    }

    private (FixtureId FixtureId, Match Match) AttachFinishedMatch(
        Stage stage,
        EntryId home,
        EntryId away,
        int homeGoals,
        int awayGoals)
    {
        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(stage.CompetitionId, stage.Id, home, away, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        Finish(match, homeGoals, awayGoals);
        return (fixture.Id, match);
    }

    private void Finish(Match match, int homeGoals, int awayGoals)
    {
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
    }

    private sealed record SelfStageContext(
        Stage Source,
        FixtureId FixtureId,
        Match Match,
        EntryId Home,
        EntryId Away);
}
