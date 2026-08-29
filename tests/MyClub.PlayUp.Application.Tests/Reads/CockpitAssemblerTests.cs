// -----------------------------------------------------------------------
// <copyright file="CockpitAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
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
        view.PreparationFocus.Should().Be(CockpitAssembler.PreparationFocusSetup);
        view.CalendarSummary.Should().BeNull();
        view.Status.Should().Be(CompetitionStatus.Draft);
        view.ConstructionDimensions.Regulation.Competition.MinimumTeams.Should().BeGreaterThan(0);
        view.ConstructionDimensions.Regulation.CompetitionRegulationMutable.Should().BeTrue();
        view.ConstructionDimensions.Regulation.TransitionReadiness.Should().NotBeEmpty();
        view.NaturalProgression.Should().BeNull(
            "Construction calm: no structural tip and no ContinueOrganisation fallback");
        view.AvailableActions.Should().Contain(action => action.Code == OrganisationViewAssembler.ActionAddEntry);

        // Entry without stage — PrepareCompetition must not be projected (Domain precondition).
        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionStartCompetition);
        view.ClosureHint.CanCompleteNormally.Should().BeFalse();
        view.Period.Should().BeNull();
    }

    [Fact]
    public void Assemble_draft_with_stage_and_active_entry_projects_PrepareCompetition()
    {
        var competition =
            Competition.Create(new CompetitionName("Ready to prepare"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.AvailableActions.Should().Contain(action =>
            action.Code == CockpitAssembler.ActionPrepareCompetition && !action.Guaranteed);
        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionStartCompetition);

        // Intentional lifecycle stays in availableActions — not naturalProgression (L7 / Option B).
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ActionPrepareStage);
    }

    [Fact]
    public void Assemble_period_is_min_max_of_match_placement_starts()
    {
        var competition = Competition.Create(new CompetitionName("Dated Cup"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "Home", _clock);
        var away = competition.AddEntry(TeamId.New(), "Away", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);

        var early = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        var late = Match.Create(competition.Id, stage.Id, away.Id, home.Id, _clock);
        stage.AddRound("R1", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        var fixtureEarly = stage.AddFixture(stage.Rounds[0].Id, _clock);
        var fixtureLate = stage.AddFixture(stage.Rounds[0].Id, _clock);
        stage.AttachMatch(fixtureEarly.Id, early.Id, legIndex: 1, _clock);
        stage.AttachMatch(fixtureLate.Id, late.Id, legIndex: 1, _clock);

        var start = new DateTimeOffset(2026, 3, 1, 15, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero);
        var resourceId = ResourceId.New();
        stage.ApplyMatchPlacements(
            [
                new MatchPlacement(early.Id, start, resourceId),
                new MatchPlacement(late.Id, end, resourceId)
            ],
            [early.Id, late.Id]);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [early, late] });

        view.Period.Should().NotBeNull();
        view.Period!.Start.Should().Be(start);
        view.Period.End.Should().Be(end);
    }

    [Fact]
    public void Assemble_draft_without_stage_does_not_project_PrepareCompetition()
    {
        var competition = Competition.Create(new CompetitionName("No stage"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Alpha", _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
    }

    [Fact]
    public void Assemble_draft_without_active_entry_does_not_project_PrepareCompetition()
    {
        var competition = Competition.Create(new CompetitionName("No entry"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
    }

    [Fact]
    public void Assemble_ready_projects_StartCompetition_not_Prepare()
    {
        var competition = Competition.Create(new CompetitionName("Ready Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.Status.Should().Be(CompetitionStatus.Ready);
        view.AvailableActions.Should().Contain(action =>
            action.Code == CockpitAssembler.ActionStartCompetition && !action.Guaranteed);
        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);

        // Ready without materialize/draw — stage still Draft → PrepareStage tip.
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ActionPrepareStage);
    }

    [Fact]
    public void Assemble_running_does_not_project_Prepare_or_Start()
    {
        var competition = Competition.Create(new CompetitionName("Running Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionStartCompetition);
    }

    [Fact]
    public void Assemble_suspended_does_not_project_Prepare_or_Start()
    {
        var competition =
            Competition.Create(new CompetitionName("Suspended Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Suspend(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
        view.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionStartCompetition);
    }

    [Fact]
    public void Assemble_completed_and_archived_do_not_project_Prepare_or_Start()
    {
        var competition = Competition.Create(new CompetitionName("Closed Cup"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        competition.Complete(CompletionMode.Administrative, _clock);

        var completed = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());
        completed.AvailableActions.Should()
            .NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
        completed.AvailableActions.Should()
            .NotContain(action => action.Code == CockpitAssembler.ActionStartCompetition);
        completed.NaturalProgression.Should().BeNull();
        completed.CycleReading.Code.Should().Be(CockpitAssembler.CycleCompleted);

        competition.Archive(_clock);
        var archived = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());
        archived.AvailableActions.Should()
            .NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
        archived.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionStartCompetition);
        archived.NaturalProgression.Should().BeNull();
        archived.CycleReading.Code.Should().Be(CockpitAssembler.CycleArchived);
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
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [scheduled, live, finished] });

        view.OperationalFocus.MatchCounts.Live.Should().Be(1);
        view.OperationalFocus.MatchCounts.Scheduled.Should().Be(1);
        view.OperationalFocus.MatchCounts.Finished.Should().Be(1);
        view.OperationalFocus.MatchCounts.Total.Should().Be(3);

        // Draft stage → no ReferenceStage → temporal units absent
        view.OperationalFocus.RecentUnit.Should().BeNull();
        view.OperationalFocus.NextUnit.Should().BeNull();
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

    [Fact]
    public void Assemble_regulation_summary_exposes_competition_bootstrap_values()
    {
        var competition = Competition.Create(new CompetitionName("Reg"), SampleRegulations.Standard(), _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        var regulation = view.ConstructionDimensions.Regulation;
        regulation.Competition.MinimumTeams.Should().Be(2);
        regulation.Competition.MaximumTeams.Should().Be(64);
        regulation.Competition.DurationPerPeriod.Should().Be(45);
        regulation.Competition.NumberOfPeriods.Should().Be(2);
        regulation.Competition.WinPoints.Should().Be(3);
        regulation.Competition.DrawPoints.Should().Be(1);
        regulation.Competition.LossPoints.Should().Be(0);
        regulation.Stage.Should().BeNull();
        regulation.CompetitionRegulationMutable.Should().BeTrue();
        regulation.TransitionReadiness.Should().Contain(item =>
            item.Transition == CockpitAssembler.TransitionDraw && !item.Ready);
        regulation.TransitionReadiness.Should().Contain(item =>
            item.Transition == CockpitAssembler.TransitionMaterializeMatches && !item.Ready);
        regulation.TransitionReadiness.Should().OnlyContain(item =>
            item.BlockerCodes.Contains(OrganisationViewAssembler.BlockerInsufficientParticipants));
    }

    [Fact]
    public void Assemble_regulation_stage_summary_and_championship_omits_draw_readiness()
    {
        var competition = CreateCompetition.Execute("Champ reg", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(2),
            _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        var regulation = view.ConstructionDimensions.Regulation;
        regulation.Stage.Should().NotBeNull();
        regulation.Stage!.StageId.Should().Be(configured.Stage.Id.Value);
        regulation.Stage.HasDrawRules.Should().BeFalse();
        regulation.Stage.HasQualificationRules.Should().BeFalse();
        regulation.Stage.HasProgressionRules.Should().BeFalse();
        regulation.Stage.HasTieFormat.Should().BeFalse();
        regulation.TransitionReadiness.Should().NotContain(item =>
            item.Transition == CockpitAssembler.TransitionDraw);
        regulation.TransitionReadiness.Should().ContainSingle(item =>
            item.Transition == CockpitAssembler.TransitionMaterializeMatches
            && item.Ready
            && item.BlockerCodes.Count == 0);
    }

    [Fact]
    public void Assemble_regulation_groups_exposes_draw_rules_and_draw_readiness()
    {
        var competition = CreateCompetition.Execute("Groups reg", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        var regulation = view.ConstructionDimensions.Regulation;
        regulation.Stage.Should().NotBeNull();
        regulation.Stage!.HasDrawRules.Should().BeTrue();
        regulation.Stage.NumberOfPots.Should().Be(2);
        regulation.TransitionReadiness.Should().Contain(item =>
            item.Transition == CockpitAssembler.TransitionDraw && item.Ready);
        regulation.TransitionReadiness.Should().Contain(item =>
            item.Transition == CockpitAssembler.TransitionMaterializeMatches);
    }

    [Fact]
    public void Assemble_regulation_readiness_empty_when_running()
    {
        var competition = Competition.Create(new CompetitionName("Running reg"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.ConstructionDimensions.Regulation.CompetitionRegulationMutable.Should().BeFalse();
        view.ConstructionDimensions.Regulation.TransitionReadiness.Should().BeEmpty();
        view.ConstructionDimensions.Regulation.Stage.Should().NotBeNull();
    }

    [Fact]
    public void Assemble_regulation_reflects_replaced_competition_values()
    {
        var competition = Competition.Create(new CompetitionName("Mut reg"), SampleRegulations.Standard(), _clock);
        var replaced = new Regulation(
            new EntryRules(minimumTeams: 4, maximumTeams: 16),
            new MatchRules(
                new MatchDuration(durationPerPeriod: 40, numberOfPeriods: 2, halfTimeDuration: 10),
                new AdministrativeResultPolicy(forfeitWinnerGoals: 3, forfeitLoserGoals: 0)),
            new StandingRules(
                new PointsPolicy(winPoints: 2, drawPoints: 1, lossPoints: 0),
                [
                    RankingCriterion.Points,
                    RankingCriterion.GoalDifference,
                    RankingCriterion.GoalsFor
                ]));
        competition.ReplaceRegulation(replaced, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.ConstructionDimensions.Regulation.Competition.MinimumTeams.Should().Be(4);
        view.ConstructionDimensions.Regulation.Competition.MaximumTeams.Should().Be(16);
        view.ConstructionDimensions.Regulation.Competition.DurationPerPeriod.Should().Be(40);
        view.ConstructionDimensions.Regulation.Competition.WinPoints.Should().Be(2);
    }

    [Fact]
    public void Assemble_projects_from_slots_action_on_secondary_cup_stage_with_occupied_slots()
    {
        var competition = CreateCompetition.Execute("Cup-D2-SF", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var qf = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;
        var sf = Stage.Create(competition.Id, new StageName("Semi-Finals"), SampleRegulations.Standard(), _clock);
        sf.AddRound("SF", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        sf.AddSlot("SF1-A", _clock);
        sf.AddSlot("SF1-B", _clock);
        sf.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);
        sf.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);
        competition.AddStage(sf.Id, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [qf, sf],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        var action = view.AvailableActions.Should().ContainSingle(item =>
            item.Code == CockpitAssembler.ActionMaterializeFromOccupiedSlots
            && item.StageId == sf.Id.Value).Subject;
        action.Params.Should().ContainKey("occupiedSlotCount").WhoseValue.Should().Be("2");
        action.Params.Should().ContainKey("stageName").WhoseValue.Should().Be("Semi-Finals");

        view.ConstructionDimensions.Regulation.TransitionReadiness.Should().Contain(item =>
            item.Transition == CockpitAssembler.TransitionMaterializeFromOccupiedSlots && item.Ready);

        // Primary Cup skeleton still incomplete → MaterializeMatches; SF from-slots is the natural next step.
        view.AvailableActions.Should().Contain(item => item.Code == CockpitAssembler.ActionMaterializeMatches);
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ActionMaterializeFromOccupiedSlots);
        view.AvailableActions.Should().NotContain(item =>
            item.Code == CockpitAssembler.ActionMaterializeFromOccupiedSlots
            && item.StageId == qf.Id.Value);
    }

    [Fact]
    public void Assemble_from_slots_absent_from_regulation_gap_when_cup_slots_empty()
    {
        var competition = CreateCompetition.Execute("Cup-NoSlotsGap", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock);
        configured.Stage.Prepare(_clock);
        configured.Stage.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.ConstructionDimensions.Regulation.TransitionReadiness.Should().NotContain(item =>
            item.Transition == CockpitAssembler.TransitionMaterializeFromOccupiedSlots);
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ActionMaterializeMatches);
    }

    [Fact]
    public void Assemble_cup_ready_for_materialization_false_after_skeleton_fixtures()
    {
        var competition = CreateCompetition.Execute("Cup-SkeletonDone", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock);
        MaterializeMatches.Execute(competition, configured.Stage, [], _clock);

        var organisation = OrganisationViewAssembler.Assemble(competition, [configured.Stage]);
        organisation.Readiness.ReadyForDraw.Should().BeTrue();
        organisation.Readiness.ReadyForMaterialization.Should().BeFalse();

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());
        view.AvailableActions.Should().NotContain(item =>
            item.Code == CockpitAssembler.ActionMaterializeMatches);
    }

    [Fact]
    public void Assemble_from_slots_absent_when_target_stage_running()
    {
        var competition = CreateCompetition.Execute("Cup-D2-RunStage", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var sf = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        sf.AddRound("SF", _clock);
        sf.AddSlot("SF1-A", _clock);
        sf.AddSlot("SF1-B", _clock);
        sf.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);
        sf.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);
        MaterializeCupFromOccupiedSlots.Execute(
            competition,
            sf,
            [new CupSlotPair("SF1-A", "SF1-B")],
            [],
            _clock);
        sf.Prepare(_clock);
        sf.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [sf],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.AvailableActions.Should().NotContain(item =>
            item.Code == CockpitAssembler.ActionMaterializeFromOccupiedSlots);
    }

    [Fact]
    public void Assemble_from_slots_when_competition_running_and_stage_draft()
    {
        var competition = CreateCompetition.Execute("Cup-D2-Late", _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddStage(StageId.New(), _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var sf = Stage.Create(competition.Id, new StageName("SF"), SampleRegulations.Standard(), _clock);
        sf.AddRound("SF", _clock);
        sf.AddSlot("SF1-A", _clock);
        sf.AddSlot("SF1-B", _clock);
        sf.ApplyResolvedEntry("SF1-A", EntryId.New(), _clock);
        sf.ApplyResolvedEntry("SF1-B", EntryId.New(), _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [sf],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.AvailableActions.Should().Contain(item =>
            item.Code == CockpitAssembler.ActionMaterializeFromOccupiedSlots
            && item.StageId == sf.Id.Value);
        view.ConstructionDimensions.Regulation.TransitionReadiness.Should().Contain(item =>
            item.Transition == CockpitAssembler.TransitionMaterializeFromOccupiedSlots && item.Ready);
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ActionMaterializeFromOccupiedSlots);
    }

    [Fact]
    public void Assemble_swiss_construction_omits_materialize_and_shows_generate_not_ready()
    {
        var competition = CreateCompetition.Execute("Swiss-Cockpit", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Swiss(3),
            _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.ConstructionDimensions.Structure.Facts["formatKind"].Should().Be("Swiss");
        view.ConstructionDimensions.Structure.Facts["swissRoundCount"].Should().Be("3");
        view.ConstructionDimensions.Regulation.TransitionReadiness.Should().NotContain(item =>
            item.Transition == CockpitAssembler.TransitionMaterializeMatches);
        view.ConstructionDimensions.Regulation.TransitionReadiness.Should().NotContain(item =>
            item.Transition == CockpitAssembler.TransitionDraw);
        var generate = view.ConstructionDimensions.Regulation.TransitionReadiness
            .Should().ContainSingle(item => item.Transition == CockpitAssembler.TransitionGenerateNextRound)
            .Subject;
        generate.Ready.Should().BeFalse();
        generate.BlockerCodes.Should().Contain(CockpitAssembler.BlockerSwissStageNotRunning);
        view.AvailableActions.Should().NotContain(item =>
            item.Code == CockpitAssembler.ActionGenerateNextRound);
        view.AvailableActions.Should().NotContain(item =>
            item.Code == CockpitAssembler.ActionMaterializeMatches);
    }

    [Fact]
    public void Assemble_swiss_running_projects_generate_next_round_and_bye()
    {
        var competition = CreateCompetition.Execute("Swiss-Run", _clock);
        for (var i = 0; i < 3; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Swiss(2),
            _clock);
        var stage = configured.Stage;
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var round1 = GenerateNextRound.Execute(competition, stage, [], _clock);
        foreach (var match in round1.CreatedMatches)
        {
            match.Start(_clock);
            match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        }

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = round1.CreatedMatches });

        view.AvailableActions.Should().Contain(item =>
            item.Code == CockpitAssembler.ActionGenerateNextRound);
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ActionGenerateNextRound);
        view.OperationalFocus.SwissByes.Should().ContainSingle();
        view.ConstructionDimensions.Structure.Facts["swissByeCount"].Should().Be("1");

        var awaiting = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [] });
        var transition = awaiting.ConstructionDimensions.Regulation.TransitionReadiness
            .Should().ContainSingle(item => item.Transition == CockpitAssembler.TransitionGenerateNextRound)
            .Subject;
        transition.Ready.Should().BeFalse();
        transition.BlockerCodes.Should().Contain(CockpitAssembler.BlockerSwissAwaitingRoundResults);
    }

    [Fact]
    public void Assemble_running_championship_without_structural_tip_projects_null_natural_progression()
    {
        var competition = Competition.Create(new CompetitionName("Calm League"), SampleRegulations.Standard(), _clock);
        var e1 = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var e2 = competition.AddEntry(TeamId.New(), "Bravo", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        _ = stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var match = Match.Create(competition.Id, stage.Id, e1.Id, e2.Id, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        view.Status.Should().Be(CompetitionStatus.Running);
        view.AvailableActions.Should().Contain(item => item.Code == CockpitAssembler.ActionStartMatch);
        view.NaturalProgression.Should().BeNull(
            "En cours calm: OpenMatches is not a tip; null is a valid business state");
    }

    [Theory]
    [InlineData(
        CockpitAssembler.ActionStartStage,
        CockpitAssembler.ActionMaterializeMatches,
        CockpitAssembler.ActionStartStage)]
    [InlineData(
        CockpitAssembler.ActionMaterializeMatches,
        CockpitAssembler.ActionPublishDraw,
        CockpitAssembler.ActionMaterializeMatches)]
    [InlineData(
        CockpitAssembler.ActionPrepareStage,
        CockpitAssembler.ActionStartStage,
        CockpitAssembler.ActionPrepareStage)]
    [InlineData(
        CockpitAssembler.ActionPublishDraw,
        CockpitAssembler.ActionApplyDraw,
        CockpitAssembler.ActionPublishDraw)]
    public void ResolveConstructionStructuralProgression_priority_is_semantic_not_list_order(
        string lowerListedFirst,
        string higherOrEqualSecond,
        string expectedWinner)
    {
        var stageId = Guid.NewGuid();
        var actions = new[]
        {
            new CockpitActionDto(higherOrEqualSecond, Guaranteed: false, stageId),
            new CockpitActionDto(lowerListedFirst, Guaranteed: false, stageId)
        };

        CockpitAssembler.ResolveConstructionStructuralProgression(actions)!
            .Code.Should().Be(expectedWinner);
    }

    [Fact]
    public void ResolveConstructionStructuralProgression_empty_actions_is_null() =>
        CockpitAssembler.ResolveConstructionStructuralProgression([]).Should().BeNull();

    [Theory]
    [InlineData(
        CockpitAssembler.ActionMaterializeFromOccupiedSlots,
        CockpitAssembler.ActionGenerateNextRound,
        CockpitAssembler.ActionMaterializeFromOccupiedSlots)]
    [InlineData(
        CockpitAssembler.ActionGenerateNextRound,
        CockpitAssembler.ActionPublishDraw,
        CockpitAssembler.ActionGenerateNextRound)]
    [InlineData(
        CockpitAssembler.ActionPublishDraw,
        CockpitAssembler.ActionApplyDraw,
        CockpitAssembler.ActionPublishDraw)]
    [InlineData(
        CockpitAssembler.ActionApplyProgression,
        CockpitAssembler.ActionCompleteCompetition,
        CockpitAssembler.ActionApplyProgression)]
    [InlineData(
        CockpitAssembler.ActionPrepareStage,
        CockpitAssembler.ActionCompleteCompetition,
        CockpitAssembler.ActionPrepareStage)]
    public void ResolveInProgressStructuralProgression_priority_is_semantic_not_list_order(
        string lowerListedFirst,
        string higherOrEqualSecond,
        string expectedWinner)
    {
        // Intentionally reverse list order vs priority: second code is lower priority when
        // lowerListedFirst wins; verifies scan uses InProgressStructuralProgressionPriority.
        var stageId = Guid.NewGuid();
        var actions = new[]
        {
            new CockpitActionDto(higherOrEqualSecond, Guaranteed: false, stageId),
            new CockpitActionDto(lowerListedFirst, Guaranteed: false, stageId)
        };

        var tip = CockpitAssembler.ResolveInProgressStructuralProgression(actions);

        tip.Should().NotBeNull();
        tip.Code.Should().Be(expectedWinner);
    }

    [Fact]
    public void ResolveInProgressStructuralProgression_empty_actions_is_null() =>
        CockpitAssembler.ResolveInProgressStructuralProgression([]).Should().BeNull();

    [Fact]
    public void Assemble_championship_projects_standing_compact_top_rows()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var e1 = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var e2 = competition.AddEntry(TeamId.New(), "Bravo", _clock);
        var e3 = competition.AddEntry(TeamId.New(), "Charlie", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var md = stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var entries = new[] { e1.Id, e2.Id, e3.Id };
        var matches = new List<Match>();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var fixture = stage.AddFixture(md.Id, _clock);
                var match = Match.Create(competition.Id, stage.Id, entries[i], entries[j], _clock);
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
                stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
                matches.Add(match);
            }
        }

        stage.Prepare(_clock);
        stage.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = matches });

        view.OperationalFocus.StandingCompact.Should().NotBeNull();
        view.OperationalFocus.StandingCompact!.StageId.Should().Be(stage.Id.Value);
        view.OperationalFocus.StandingCompact.Tables.Should().ContainSingle();
        var table = view.OperationalFocus.StandingCompact.Tables[0];
        table.Scope.Should().Be(ConsultationAssembler.ScopeOverall);
        table.Rows.Should().HaveCount(3);
        table.Rows[0].Position.Should().Be(1);
        table.Rows.Should().OnlyContain(row =>
            !string.IsNullOrWhiteSpace(row.DisplayName) && row.Played >= 0);
        view.OperationalFocus.RecentUnit.Should().NotBeNull();
        view.OperationalFocus.RecentUnit!.MatchdayNumber.Should().Be(1);
        view.OperationalFocus.RecentUnit.MatchCount.Should().Be(3);
        view.OperationalFocus.RecentUnit.Matches.Should().OnlyContain(line =>
            line.Status == MatchStatus.Finished && line.Score != null);
        view.OperationalFocus.NextUnit.Should().BeNull();
        view.OperationalFocus.ReferenceStageGameRules.Should().NotBeNull();
        view.OperationalFocus.ReferenceStageGameRules!.FormatKind.Should().Be("Championship");
        view.OperationalFocus.ReferenceStageGameRules.WinPoints.Should().Be(3);
        view.OperationalFocus.ReferenceStageGameRules.NumberOfPeriods.Should().Be(2);
        view.CompetitionOutcome.Should().BeNull();
    }

    [Fact]
    public void Assemble_completed_championship_projects_full_competition_outcome_from_standing()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var e1 = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var e2 = competition.AddEntry(TeamId.New(), "Bravo", _clock);
        var e3 = competition.AddEntry(TeamId.New(), "Charlie", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var md = stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var entries = new[] { e1.Id, e2.Id, e3.Id };
        var matches = new List<Match>();
        for (var i = 0; i < entries.Length; i++)
        {
            for (var j = i + 1; j < entries.Length; j++)
            {
                var fixture = stage.AddFixture(md.Id, _clock);
                var match = Match.Create(competition.Id, stage.Id, entries[i], entries[j], _clock);
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
                stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
                matches.Add(match);
            }
        }

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);
        competition.Complete(CompletionMode.Normal, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = matches });

        view.CycleReading.Code.Should().Be(CockpitAssembler.CycleCompleted);
        view.CompetitionOutcome.Should().NotBeNull();
        view.CompetitionOutcome!.Places.Should().HaveCount(3);
        view.CompetitionOutcome.Places[0].Rank.Should().Be(1);
        view.CompetitionOutcome.Places[0].DisplayName.Should().Be("Alpha");
        view.CompetitionOutcome.Places.Select(place => place.EntryId).Should().OnlyContain(id =>
            id == e1.Id.Value || id == e2.Id.Value || id == e3.Id.Value);
        view.CompetitionOutcome.Places.Should().OnlyContain(place =>
            !string.IsNullOrWhiteSpace(place.DisplayName));
    }

    [Fact]
    public void Assemble_completed_cup_projects_outcome_from_placement_awards()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var alpha = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var bravo = competition.AddEntry(TeamId.New(), "Bravo", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("Final", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var match = Match.Create(competition.Id, stage.Id, alpha.Id, bravo.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath(fixture.Id, ProgressionOutcome.Winner, 1),
                new PlacementAwardPath(fixture.Id, ProgressionOutcome.Loser, 2)
            ]),
            _clock);

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);
        competition.Complete(CompletionMode.Normal, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        view.CompetitionOutcome.Should().NotBeNull();
        view.CompetitionOutcome!.Places.Should().HaveCount(2);
        view.CompetitionOutcome.Places[0].Should().Be(new FinalPlacementDto(1, alpha.Id.Value, "Alpha"));
        view.CompetitionOutcome.Places[1].Should().Be(new FinalPlacementDto(2, bravo.Id.Value, "Bravo"));
    }

    [Fact]
    public void Assemble_completed_cup_projects_partial_outcome_when_bronze_undecided()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var a = competition.AddEntry(TeamId.New(), "A", _clock);
        var b = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("KO"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("Finals", _clock);
        var final = stage.AddFixture(round.Id, _clock);
        var bronze = stage.AddFixture(round.Id, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var finalMatch = Match.Create(competition.Id, stage.Id, a.Id, b.Id, _clock);
        finalMatch.Start(_clock);
        finalMatch.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        stage.AttachMatch(final.Id, finalMatch.Id, legIndex: 1, _clock);
        stage.ReplacePlacementAwardRules(
            new PlacementAwardRules(
            [
                new PlacementAwardPath(final.Id, ProgressionOutcome.Winner, 1),
                new PlacementAwardPath(final.Id, ProgressionOutcome.Loser, 2),
                new PlacementAwardPath(bronze.Id, ProgressionOutcome.Winner, 3),
                new PlacementAwardPath(bronze.Id, ProgressionOutcome.Loser, 4)
            ]),
            _clock);

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);
        competition.Complete(CompletionMode.Normal, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [finalMatch] });

        view.CompetitionOutcome.Should().NotBeNull();
        view.CompetitionOutcome!.Places.Select(p => p.Rank).Should().Equal(1, 2);
        view.CompetitionOutcome.Places.Should().NotContain(p => p.Rank == 3 || p.Rank == 4);
    }

    [Fact]
    public void Assemble_completed_cup_without_placement_rules_has_no_outcome()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var a = competition.AddEntry(TeamId.New(), "A", _clock);
        var b = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Final"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("Final", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var match = Match.Create(competition.Id, stage.Id, a.Id, b.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);
        competition.Complete(CompletionMode.Normal, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        view.CompetitionOutcome.Should().BeNull();
    }

    [Fact]
    public void Assemble_abandoned_championship_does_not_project_competition_outcome()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var e1 = competition.AddEntry(TeamId.New(), "Alpha", _clock);
        var e2 = competition.AddEntry(TeamId.New(), "Bravo", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var md = stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var fixture = stage.AddFixture(md.Id, _clock);
        var match = Match.Create(competition.Id, stage.Id, e1.Id, e2.Id, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Complete(CompletionMode.Abandoned, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        view.CompetitionOutcome.Should().BeNull();
    }

    [Fact]
    public void Assemble_completed_groups_does_not_project_competition_outcome()
    {
        var competition = Competition.Create(new CompetitionName("Groups"), SampleRegulations.Standard(), _clock);
        var a = competition.AddEntry(TeamId.New(), "A", _clock);
        var b = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var g1 = stage.AddGroup("G1", _clock);
        stage.AssignEntryToGroup(g1.Id, a.Id, _clock);
        stage.AssignEntryToGroup(g1.Id, b.Id, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var md = stage.AddMatchday(1, _clock);
        var f1 = stage.AddFixture(md.Id, _clock);
        var m1 = Match.Create(competition.Id, stage.Id, a.Id, b.Id, _clock);
        m1.Start(_clock);
        m1.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        stage.AttachMatch(f1.Id, m1.Id, legIndex: 1, _clock);

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);
        competition.Complete(CompletionMode.Normal, _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [m1] });

        view.CompetitionOutcome.Should().BeNull();
        view.OperationalFocus.StandingCompact.Should().NotBeNull();
    }

    [Fact]
    public void Assemble_groups_standing_compact_exposes_all_group_tables()
    {
        var competition = Competition.Create(new CompetitionName("Groups"), SampleRegulations.Standard(), _clock);
        var a = competition.AddEntry(TeamId.New(), "A", _clock);
        var b = competition.AddEntry(TeamId.New(), "B", _clock);
        var c = competition.AddEntry(TeamId.New(), "C", _clock);
        var d = competition.AddEntry(TeamId.New(), "D", _clock);
        var stage = Stage.Create(competition.Id, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var g1 = stage.AddGroup("G1", _clock);
        stage.AssignEntryToGroup(g1.Id, a.Id, _clock);
        stage.AssignEntryToGroup(g1.Id, b.Id, _clock);
        var g2 = stage.AddGroup("G2", _clock);
        stage.AssignEntryToGroup(g2.Id, c.Id, _clock);
        stage.AssignEntryToGroup(g2.Id, d.Id, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var md = stage.AddMatchday(1, _clock);
        var f1 = stage.AddFixture(md.Id, _clock);
        var m1 = Match.Create(competition.Id, stage.Id, a.Id, b.Id, _clock);
        m1.Start(_clock);
        m1.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
        stage.AttachMatch(f1.Id, m1.Id, legIndex: 1, _clock);

        stage.Prepare(_clock);
        stage.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [m1] });

        view.OperationalFocus.StandingCompact.Should().NotBeNull();
        view.OperationalFocus.StandingCompact!.Tables.Should().HaveCount(2);
        view.OperationalFocus.StandingCompact.Tables.Should().OnlyContain(table =>
            table.Scope == ConsultationAssembler.ScopeGroup && table.GroupId != null);
    }

    [Fact]
    public void ResolveReferenceStage_prefers_first_running_over_completed()
    {
        var competition = Competition.Create(new CompetitionName("Multi"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var completed = Stage.Create(competition.Id, new StageName("Done"), SampleRegulations.Standard(), _clock);
        var running = Stage.Create(competition.Id, new StageName("Live"), SampleRegulations.Standard(), _clock);
        completed.AddMatchday(1, _clock);
        running.AddMatchday(1, _clock);
        competition.AddStage(completed.Id, _clock);
        competition.AddStage(running.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        completed.Prepare(_clock);
        completed.Start(_clock);
        completed.Complete(_clock);
        running.Prepare(_clock);
        running.Start(_clock);

        var reference = CockpitAssembler.ResolveReferenceStage(competition, [completed, running]);

        reference.Should().NotBeNull();
        reference.Id.Should().Be(running.Id);
    }

    [Fact]
    public void ResolveReferenceStage_treats_suspended_as_running()
    {
        var competition = Competition.Create(new CompetitionName("Multi"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var completed = Stage.Create(competition.Id, new StageName("Done"), SampleRegulations.Standard(), _clock);
        var suspended = Stage.Create(competition.Id, new StageName("Paused"), SampleRegulations.Standard(), _clock);
        completed.AddMatchday(1, _clock);
        suspended.AddMatchday(1, _clock);
        competition.AddStage(completed.Id, _clock);
        competition.AddStage(suspended.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        completed.Prepare(_clock);
        completed.Start(_clock);
        completed.Complete(_clock);
        suspended.Prepare(_clock);
        suspended.Start(_clock);
        suspended.Suspend(_clock);

        var reference = CockpitAssembler.ResolveReferenceStage(competition, [completed, suspended]);

        reference.Should().NotBeNull();
        reference.Id.Should().Be(suspended.Id);
        reference.Status.Should().Be(StageStatus.Suspended);
    }

    [Fact]
    public void ResolveReferenceStage_falls_back_to_last_completed()
    {
        var competition = Competition.Create(new CompetitionName("Multi"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var first = Stage.Create(competition.Id, new StageName("S1"), SampleRegulations.Standard(), _clock);
        var second = Stage.Create(competition.Id, new StageName("S2"), SampleRegulations.Standard(), _clock);
        first.AddMatchday(1, _clock);
        second.AddMatchday(1, _clock);
        competition.AddStage(first.Id, _clock);
        competition.AddStage(second.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        first.Prepare(_clock);
        first.Start(_clock);
        first.Complete(_clock);
        second.Prepare(_clock);
        second.Start(_clock);
        second.Complete(_clock);

        var reference = CockpitAssembler.ResolveReferenceStage(competition, [first, second]);

        reference.Should().NotBeNull();
        reference.Id.Should().Be(second.Id);
    }

    [Fact]
    public void Assemble_cup_omits_standing_compact()
    {
        var (competition, stage, match) = CreateFinishedKnockoutWithProgression();
        competition.Prepare(_clock);
        competition.Start(_clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [match] });

        view.OperationalFocus.StandingCompact.Should().BeNull();
        view.OperationalFocus.RecentUnit.Should().NotBeNull();
        view.OperationalFocus.RecentUnit!.UnitKind.Should().Be(CockpitAssembler.UnitKindRound);
        view.OperationalFocus.RecentUnit.RoundName.Should().Be("R1");
        view.OperationalFocus.RecentUnit.Matches.Should().ContainSingle(line =>
            line.MatchId == match.Id.Value &&
            line.Score != null &&
            line.Score.HomeGoals == 2 &&
            line.Score.AwayGoals == 1);
        view.OperationalFocus.NextUnit.Should().BeNull();
        view.OperationalFocus.ReferenceStageGameRules.Should().NotBeNull();
        view.OperationalFocus.ReferenceStageGameRules!.FormatKind.Should().Be("Cup");
        view.OperationalFocus.ReferenceStageGameRules.NumberOfLegs.Should().Be(1);
    }

    [Fact]
    public void Assemble_temporal_units_mixed_matchday_and_next()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var a = competition.AddEntry(TeamId.New(), "A", _clock);
        var b = competition.AddEntry(TeamId.New(), "B", _clock);
        var c = competition.AddEntry(TeamId.New(), "C", _clock);
        var d = competition.AddEntry(TeamId.New(), "D", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var md4 = stage.AddMatchday(4, _clock);
        var md5 = stage.AddMatchday(5, _clock);
        var md6 = stage.AddMatchday(6, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var matches = new List<Match>
        {
            // J4 — all finished
            AttachFinished(competition, stage, md4.Id, a.Id, b.Id, 1, 0),
            AttachFinished(competition, stage, md4.Id, c.Id, d.Id, 2, 1),

            // J5 — finished + live + scheduled
            AttachFinished(competition, stage, md5.Id, a.Id, c.Id, 1, 1)
        };

        var live = Match.Create(competition.Id, stage.Id, b.Id, d.Id, _clock);
        live.Start(_clock);
        stage.AttachMatch(stage.AddFixture(md5.Id, _clock).Id, live.Id, legIndex: 1, _clock);
        matches.Add(live);
        var scheduledJ5 = Match.Create(competition.Id, stage.Id, a.Id, d.Id, _clock);
        stage.AttachMatch(stage.AddFixture(md5.Id, _clock).Id, scheduledJ5.Id, legIndex: 1, _clock);
        matches.Add(scheduledJ5);

        // J6 — all scheduled
        var scheduledJ6 = Match.Create(competition.Id, stage.Id, b.Id, c.Id, _clock);
        stage.AttachMatch(stage.AddFixture(md6.Id, _clock).Id, scheduledJ6.Id, legIndex: 1, _clock);
        matches.Add(scheduledJ6);

        stage.Prepare(_clock);
        stage.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = matches });

        view.OperationalFocus.RecentUnit.Should().NotBeNull();
        view.OperationalFocus.RecentUnit!.MatchdayNumber.Should().Be(5);
        view.OperationalFocus.RecentUnit.MatchCount.Should().Be(3);
        view.OperationalFocus.RecentUnit.Matches.Should().Contain(line => line.Status == MatchStatus.Live);
        view.OperationalFocus.RecentUnit.Matches.Should().Contain(line => line.Status == MatchStatus.Scheduled);
        view.OperationalFocus.RecentUnit.Matches.Should().Contain(line => line.Status == MatchStatus.Finished);
        view.OperationalFocus.RecentUnit.Matches[0].Status.Should().Be(MatchStatus.Live);

        view.OperationalFocus.NextUnit.Should().NotBeNull();
        view.OperationalFocus.NextUnit!.MatchdayNumber.Should().Be(6);
        view.OperationalFocus.NextUnit.Matches.Should().OnlyContain(line => line.Status == MatchStatus.Scheduled);
    }

    [Fact]
    public void Assemble_temporal_units_before_kickoff_recent_empty_next_first()
    {
        var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        var md1 = stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var scheduled = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        stage.AttachMatch(stage.AddFixture(md1.Id, _clock).Id, scheduled.Id, legIndex: 1, _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = [scheduled] });

        view.OperationalFocus.RecentUnit.Should().BeNull();
        view.OperationalFocus.NextUnit.Should().NotBeNull();
        view.OperationalFocus.NextUnit!.MatchdayNumber.Should().Be(1);
        view.OperationalFocus.NextUnit.Matches.Should().ContainSingle(line =>
            line.MatchId == scheduled.Id.Value && line.Status == MatchStatus.Scheduled);
    }

    [Fact]
    public void Assemble_swiss_without_next_round_next_unit_null()
    {
        var competition = CreateCompetition.Execute("Swiss-Next", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Swiss(3),
            _clock);
        var stage = configured.Stage;
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var round1 = GenerateNextRound.Execute(competition, stage, [], _clock);
        foreach (var match in round1.CreatedMatches)
        {
            match.Start(_clock);
            match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        }

        var view = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>> { [stage.Id] = round1.CreatedMatches });

        view.OperationalFocus.RecentUnit.Should().NotBeNull();
        view.OperationalFocus.RecentUnit!.UnitKind.Should().Be(CockpitAssembler.UnitKindMatchday);
        view.OperationalFocus.RecentUnit.MatchdayNumber.Should().Be(1);
        view.OperationalFocus.NextUnit.Should().BeNull();
    }

    [Fact]
    public void Assemble_championship_ready_with_matches_projects_GeneratedCalendar()
    {
        var competition = CreateCompetition.Execute("Champ-Cal", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock);
        var materialize = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        competition.Prepare(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>
            {
                [configured.Stage.Id] = materialize.CreatedMatches
            });

        view.CycleReading.Code.Should().Be(CockpitAssembler.CycleConstruction);
        view.Status.Should().Be(CompetitionStatus.Ready);
        view.PreparationFocus.Should().Be(CockpitAssembler.PreparationFocusGeneratedCalendar);
        view.CalendarSummary.Should().NotBeNull();
        view.CalendarSummary!.MatchCount.Should().Be(6);
        view.CalendarSummary.MatchdayCount.Should().BeGreaterThan(0);
        view.CalendarSummary.Matchdays.Should().NotBeEmpty();
        view.CalendarSummary.Matchdays.Count.Should().BeLessThanOrEqualTo(
            CockpitAssembler.CalendarPreviewMatchdayLimit);
        view.CalendarSummary.NextMatch.Should().NotBeNull();
        view.CalendarSummary.NextMatch!.HomeDisplayName.Should().NotBeNullOrWhiteSpace();
        view.AvailableActions.Should().Contain(action =>
            action.Code == CockpitAssembler.ActionStartCompetition);
    }

    [Fact]
    public void Assemble_championship_draft_with_matches_stays_Setup()
    {
        var competition = CreateCompetition.Execute("Champ-Draft-Matches", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(),
            _clock);
        var materialize = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>
            {
                [configured.Stage.Id] = materialize.CreatedMatches
            });

        view.Status.Should().Be(CompetitionStatus.Draft);
        view.PreparationFocus.Should().Be(CockpitAssembler.PreparationFocusSetup);
        view.CalendarSummary.Should().BeNull();
    }

    [Fact]
    public void Assemble_championship_ready_without_matches_stays_Setup()
    {
        var competition = CreateCompetition.Execute("Champ-Ready-Empty", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Championship(2),
            _clock);
        competition.Prepare(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());

        view.Status.Should().Be(CompetitionStatus.Ready);
        view.PreparationFocus.Should().Be(CockpitAssembler.PreparationFocusSetup);
        view.CalendarSummary.Should().BeNull();
    }

    [Fact]
    public void Assemble_cup_ready_with_skeleton_matches_stays_Setup()
    {
        var competition = CreateCompetition.Execute("Cup-Ready-Skeleton", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        AddEntry.Execute(competition, "C", _clock);
        AddEntry.Execute(competition, "D", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock);
        var materialize = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
        competition.Prepare(_clock);

        var view = CockpitAssembler.Assemble(
            competition,
            [configured.Stage],
            new Dictionary<StageId, IReadOnlyList<Match>>
            {
                [configured.Stage.Id] = materialize.CreatedMatches
            });

        view.Status.Should().Be(CompetitionStatus.Ready);
        view.PreparationFocus.Should().Be(CockpitAssembler.PreparationFocusSetup);
        view.CalendarSummary.Should().BeNull();
    }

    [Fact]
    public void ResolvePreparationFocus_requires_all_championship_ready_predicates()
    {
        CockpitAssembler.ResolvePreparationFocus(
                CockpitAssembler.CycleConstruction,
                CompetitionStatus.Ready,
                StructureFormatKind.Championship,
                matchTotal: 1)
            .Should().Be(CockpitAssembler.PreparationFocusGeneratedCalendar);

        CockpitAssembler.ResolvePreparationFocus(
                CockpitAssembler.CycleConstruction,
                CompetitionStatus.Draft,
                StructureFormatKind.Championship,
                matchTotal: 1)
            .Should().Be(CockpitAssembler.PreparationFocusSetup);

        CockpitAssembler.ResolvePreparationFocus(
                CockpitAssembler.CycleInProgress,
                CompetitionStatus.Running,
                StructureFormatKind.Championship,
                matchTotal: 1)
            .Should().Be(CockpitAssembler.PreparationFocusSetup);

        CockpitAssembler.ResolvePreparationFocus(
                CockpitAssembler.CycleConstruction,
                CompetitionStatus.Ready,
                StructureFormatKind.Swiss,
                matchTotal: 1)
            .Should().Be(CockpitAssembler.PreparationFocusSetup);
    }

    private Match AttachFinished(
        Competition competition,
        Stage stage,
        MatchdayId matchdayId,
        EntryId home,
        EntryId away,
        int homeGoals,
        int awayGoals)
    {
        var fixture = stage.AddFixture(matchdayId, _clock);
        var match = Match.Create(competition.Id, stage.Id, home, away, _clock);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(homeGoals, awayGoals)), _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        return match;
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
