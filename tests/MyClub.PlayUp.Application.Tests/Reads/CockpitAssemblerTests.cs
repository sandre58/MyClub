// -----------------------------------------------------------------------
// <copyright file="CockpitAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class CockpitAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 20, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_draft_exposes_construction_cycle_and_organisation_actions()
    {
        var competition = Competition.Create(new CompetitionName("Draft Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Alpha", _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.CycleReading.Code.Should().Be(CockpitAssembler.CycleConstruction);
        view.Status.Should().Be(CompetitionStatus.Draft);
        view.ConstructionDimensions.Regulation.Facts.MinimumTeams.Should().BeGreaterThan(0);
        view.NaturalProgression.Should().NotBeNull();
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ProgressionContinueOrganisation);
        view.AvailableActions.Should().Contain(action => action.Code == OrganisationViewAssembler.ActionAddEntry);
        view.AvailableActions.Should().NotContain(action => action.Code == "PrepareCompetition");
        view.AvailableActions.Should().NotContain(action => action.Code == "StartCompetition");
        view.ClosureHint.CanCompleteNormally.Should().BeFalse();
    }

    [Fact]
    public void Assemble_includes_org_blockers_as_blocking_situations_in_attention()
    {
        var competition = Competition.Create(new CompetitionName("Thin"), SampleRegulations.Standard(), _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.Situations.Should().Contain(item =>
            item.Source == OrganisationViewAssembler.BlockerInsufficientParticipants
            && item.Nature == CockpitAssembler.NatureBlocking
            && item.Actionable
            && item.ActionCode == OrganisationViewAssembler.ActionAddEntry
            && item.ImpactCode == CockpitAssembler.ImpactBlocksConstruction);
        view.AttentionSummary.Count.Should().Be(view.AttentionSummary.Items.Count);
        view.AttentionSummary.Items.Should().OnlyContain(item => item.Nature == CockpitAssembler.NatureBlocking);
    }

    [Fact]
    public void Assemble_does_not_project_org_blockers_as_situations_when_running()
    {
        var competition = Competition.Create(new CompetitionName("Running thin"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Only", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.Situations.Should().NotContain(item =>
            item.Source == OrganisationViewAssembler.BlockerInsufficientParticipants);
    }

    [Fact]
    public void Assemble_detects_draw_no_solution_blocking_not_actionable()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.MarkDrawNoSolution(draw.Id, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        var noSolution = view.Situations.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceDrawNoSolution).Subject;
        noSolution.Nature.Should().Be(CockpitAssembler.NatureBlocking);
        noSolution.Actionable.Should().BeFalse();
        noSolution.ActionCode.Should().BeNull();
        noSolution.ImpactCode.Should().Be(CockpitAssembler.ImpactBlocksDraw);
        noSolution.TargetType.Should().Be("Draw");
        noSolution.TargetId.Should().Be(draw.Id.Value.ToString());
        view.AttentionSummary.Items.Should().Contain(item =>
            item.Source == NeedsAttentionAssembler.SourceDrawNoSolution);
        view.OperationalFocus.Draws.Should().ContainSingle(item =>
            item.DrawId == draw.Id.Value
            && item.ResolutionState == DrawResolutionState.NoSolution
            && !item.IsApplied);
    }

    [Fact]
    public void Assemble_marks_slot_draw_applied_when_placements_match_slots()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("SF1-A", _clock);
        competition.AddStage(stage.Id, _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([home.Id]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(home.Id, "SF1-A")]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        stage.ApplyResolvedEntry("SF1-A", home.Id, _clock);

        DrawAppliedState.IsApplied(stage.Draws.Single(), stage).Should().BeTrue();

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.OperationalFocus.Draws.Should().ContainSingle(item => item.IsApplied);
        view.AvailableActions.Should().NotContain(action =>
            action.Code == CockpitAssembler.ActionApplyDraw && action.DrawId == draw.Id.Value);
    }

    [Fact]
    public void Assemble_resolves_fixture_to_match_on_progression_situation()
    {
        var ctx = CreateFinishedKnockoutWithProgression();

        var view = CockpitAssembler.Assemble(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        var progression = view.Situations.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceProgressionPending).Subject;
        progression.MatchId.Should().Be(ctx.Match.Id.Value);
        progression.Nature.Should().Be(CockpitAssembler.NatureBlocking);
        progression.Actionable.Should().BeTrue();
        progression.ActionCode.Should().Be(CockpitAssembler.ActionApplyProgression);
        progression.ImpactCode.Should().Be(CockpitAssembler.ImpactBlocksProgression);
        view.NavigationHints.Should().Contain(hint =>
            hint.TargetType == "Fixture" && hint.MatchId == ctx.Match.Id.Value);
        view.AvailableActions.Should().Contain(action => action.Code == CockpitAssembler.ActionApplyProgression);
    }

    [Fact]
    public void Assemble_progression_conflict_is_blocking_and_actionable()
    {
        var ctx = CreateFinishedKnockoutWithProgression();
        ctx.Stage.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);

        var view = CockpitAssembler.Assemble(
            ctx.Competition,
            [ctx.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [ctx.Stage.Id] = [ctx.Match] });

        var conflict = view.Situations.Should().ContainSingle(item =>
            item.Source == NeedsAttentionAssembler.SourceProgressionConflict).Subject;
        conflict.Nature.Should().Be(CockpitAssembler.NatureBlocking);
        conflict.Actionable.Should().BeTrue();
        conflict.ActionCode.Should().Be(CockpitAssembler.ActionApplyProgression);
        conflict.ImpactCode.Should().Be(CockpitAssembler.ImpactBlocksProgression);
    }

    [Fact]
    public void Assemble_suspended_is_informational_not_in_attention_and_not_actionable()
    {
        var competition = Competition.Create(new CompetitionName("Paused"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Suspend(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        var suspended = view.Situations.Should().ContainSingle(item =>
            item.Source == CockpitAssembler.SourceCompetitionSuspended).Subject;
        suspended.Nature.Should().Be(CockpitAssembler.NatureInformational);
        suspended.Actionable.Should().BeFalse();
        suspended.ActionCode.Should().BeNull();
        suspended.ImpactCode.Should().BeNull();
        view.AttentionSummary.Items.Should().NotContain(item =>
            item.Source == CockpitAssembler.SourceCompetitionSuspended);
        view.AvailableActions.Should().NotContain(action => action.Code == "ResumeCompetition");
    }

    [Fact]
    public void Assemble_does_not_treat_finished_match_alone_as_situation()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        view.Situations.Should().BeEmpty();
        view.AttentionSummary.Count.Should().Be(0);
    }

    [Fact]
    public void Assemble_situations_have_stable_identity_and_no_duplicates()
    {
        var competition = Competition.Create(new CompetitionName("Thin"), SampleRegulations.Standard(), _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        var keys = view.Situations
            .Select(item => $"{item.Source}|{item.TargetType}|{item.TargetId}")
            .ToList();
        keys.Should().OnlyHaveUniqueItems();
        view.Situations.Should().OnlyContain(item =>
            !string.IsNullOrWhiteSpace(item.Source));
    }

    [Fact]
    public void Assemble_match_counts_and_prepare_start_stage_actions()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);

        var scheduled = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        var live = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        live.Start(_clock);
        var finished = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        finished.Start(_clock);
        finished.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>
            {
                [stage.Id] = [scheduled, live, finished]
            });

        view.OperationalFocus.MatchCounts.Live.Should().Be(1);
        view.OperationalFocus.MatchCounts.Scheduled.Should().Be(1);
        view.OperationalFocus.MatchCounts.Finished.Should().Be(1);
        view.OperationalFocus.MatchCounts.Total.Should().Be(3);
        view.AvailableActions.Should().Contain(action => action.Code == CockpitAssembler.ActionPrepareStage);
        view.AvailableActions.Should().Contain(action =>
            action.Code == CockpitAssembler.ActionStartMatch && action.MatchId == scheduled.Id.Value);
        view.AvailableActions.Should().Contain(action =>
            action.Code == CockpitAssembler.ActionFinishMatch && action.MatchId == live.Id.Value);
    }

    [Fact]
    public void Assemble_closure_uses_completion_analyzer_distinct_from_attention()
    {
        var competition = Competition.Create(new CompetitionName("Running"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var scheduled = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [scheduled] });

        view.CycleReading.Code.Should().Be(CockpitAssembler.CycleInProgress);
        view.ClosureHint.CanCompleteNormally.Should().BeFalse();
        view.ClosureHint.BlockerCodes.Should().Contain(CompletionAnalyzer.ReasonScheduledMatches);
        view.AttentionSummary.Items.Should().NotContain(item =>
            item.Source == CompletionAnalyzer.ReasonScheduledMatches);
    }

    private (Competition Competition, Stage Stage, Match Match) CreateFinishedKnockoutWithProgression()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("QF"), SampleRegulations.Standard(), _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        stage.AddSlot("SF1-A", _clock);
        competition.AddStage(stage.Id, _clock);

        var fixture = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
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

        return (competition, stage, match);
    }
}
