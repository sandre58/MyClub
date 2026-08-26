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
        view.Status.Should().Be(CompetitionStatus.Draft);
        view.ConstructionDimensions.Regulation.Competition.MinimumTeams.Should().BeGreaterThan(0);
        view.ConstructionDimensions.Regulation.CompetitionRegulationMutable.Should().BeTrue();
        view.ConstructionDimensions.Regulation.TransitionReadiness.Should().NotBeEmpty();
        view.NaturalProgression.Should().NotBeNull();
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ProgressionContinueOrganisation);
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
        var competition = Competition.Create(new CompetitionName("Ready to prepare"), SampleRegulations.Standard(), _clock);
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
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ProgressionContinueOrganisation);
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

        // Ready without materialize/draw readiness — operational tip unchanged (Option B).
        view.NaturalProgression!.Code.Should().Be(CockpitAssembler.ProgressionContinueOrganisation);
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
        var competition = Competition.Create(new CompetitionName("Suspended Cup"), SampleRegulations.Standard(), _clock);
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
        completed.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
        completed.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionStartCompetition);

        competition.Archive(_clock);
        var archived = CockpitAssembler.Assemble(
            competition,
            [stage],
            new Dictionary<StageId, IReadOnlyList<Match>>());
        archived.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionPrepareCompetition);
        archived.AvailableActions.Should().NotContain(action => action.Code == CockpitAssembler.ActionStartCompetition);
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
