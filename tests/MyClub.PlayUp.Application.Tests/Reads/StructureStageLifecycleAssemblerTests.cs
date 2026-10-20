// -----------------------------------------------------------------------
// <copyright file="StructureStageLifecycleAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureStageLifecycleAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 10, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_Draft_championship_projects_Prepare_enabled()
    {
        var situation = TestCompetition.Create("Lifecycle Draft", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"));
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle.Should().NotBeNull();
        hub.Lifecycle.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        hub.Lifecycle.Transition.Enabled.Should().BeTrue();
        hub.Lifecycle.MatchProgress.Should().BeNull();
    }

    [Fact]
    public void Assemble_Draft_groups_with_empty_schematic_and_composition_allows_Prepare()
    {
        var situation = TestCompetition.Create("Lifecycle Draft groups", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2, "Poules"));

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.StructureIssues.Should().Contain(CompetitionFlowAnalyzer.SlotFeedMissing);
        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
        PrepareStage.Execute(situation.PrimaryStage!, situation.Stages, _clock);
        situation.PrimaryStage!.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Assemble_Draft_championship_with_partial_schematic_fill_allows_Prepare()
    {
        var situation = TestCompetition.Create("Lifecycle Draft partial", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"));
        var stage = situation.PrimaryStage!;
        stage.ClearCompositionEntries(_clock);
        foreach (var entry in situation.Competition.Entries.Take(2))
        {
            stage.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.StructureIssues.Should().Contain(CompetitionFlowAnalyzer.SlotFeedMissing);
        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        hub.Lifecycle.Transition.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Assemble_Ready_without_teams_blocks_Start_until_roster_and_matches()
    {
        var situation = TestCompetition.Create("Lifecycle Ready", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete);
    }

    [Fact]
    public void Assemble_Ready_with_unmaterialized_championship_blocks_Start()
    {
        var situation = TestCompetition.Create("Lifecycle Ready Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
    }

    [Fact]
    public void Assemble_Ready_groups_partial_production_blocks_Start_with_matchs_remediation()
    {
        var situation = TestCompetition.Create("Groups partial production", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2, "Poules"));
        var stage = situation.PrimaryStage!;
        var entries = situation.Competition.Entries.OrderBy(entry => entry.Id.Value).ToArray();
        stage.AssignEntryToGroup(stage.Groups[0].Id, entries[0].Id);
        stage.AssignEntryToGroup(stage.Groups[0].Id, entries[1].Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, entries[2].Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, entries[3].Id);
        situation.PreparePrimaryStage();

        stage.Status.Should().Be(StageStatus.Ready);

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle.Should().NotBeNull();
        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);

        hub.Lifecycle.Transition.Remediation.Should().NotBeNull();
        hub.Lifecycle.Transition.Remediation!.Href.Should().Contain("section=matchs");
        hub.Lifecycle.Transition.Remediation.Href.Should().NotContain("operational-placement");

        hub.Materialize.Should().NotBeNull();
        hub.Materialize!.CommandEnabled.Should().BeTrue();
        hub.Materialize.EligibleCount.Should().Be(2);
        hub.Materialize.DeterminedCount.Should().Be(2);
        hub.Materialize.MaterializedCount.Should().Be(0);
        hub.Materialize.CommandCode.Should().Be(OverviewAssembler.ActionMaterializeMatches);
    }

    [Fact]
    public void Assemble_Ready_cup_partial_production_blocks_Start_with_matchs_remediation()
    {
        var situation = TestCompetition.Create("Cup partial production", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        var entries = situation.Competition.Entries.OrderBy(entry => entry.Id.Value).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0].Id, _clock);
        stage.ApplyResolvedEntry("S2", entries[1].Id, _clock);
        situation.PreparePrimaryStage();

        stage.Status.Should().Be(StageStatus.Ready);

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle.Should().NotBeNull();
        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);

        hub.Lifecycle.Transition.Remediation.Should().NotBeNull();
        hub.Lifecycle.Transition.Remediation!.Href.Should().Contain("focus=operational-placement");
        hub.Lifecycle.Transition.Remediation.Href.Should().NotContain("section=matchs");

        hub.Materialize.Should().NotBeNull();
        hub.Materialize!.CommandEnabled.Should().BeTrue();
        hub.Materialize.EligibleCount.Should().Be(1);
        hub.Materialize.DeterminedCount.Should().Be(1);
        hub.Materialize.MaterializedCount.Should().Be(0);
        hub.Materialize.CommandCode.Should().Be(OverviewAssembler.ActionMaterializeFromOccupiedSlots);
        hub.Materialize.DisabledReason.Should().BeNull();
        hub.Materialize.Confrontations.Should().ContainSingle(row =>
            row.Determined && row.Eligible && !row.Materialized);
    }

    [Fact]
    public void Assemble_Running_with_open_match_blocks_Complete()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Scheduled);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionCompleteStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should().Be(CompleteStage.ReasonOpenMatches);
        hub.Lifecycle.Transition.Problem.Should().NotBeNull();
        hub.Lifecycle.Transition.Problem!.CanonCode.Should().Be(CompleteStage.ReasonOpenMatches);
        hub.Lifecycle.MatchProgress!.Open.Should().Be(1);
        hub.Lifecycle.MatchProgress.CanComplete.Should().BeFalse();
    }

    [Fact]
    public void Assemble_Running_with_finished_match_allows_Complete()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Finished);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionCompleteStage);
        hub.Lifecycle.Transition.Enabled.Should().BeTrue();
        hub.Lifecycle.MatchProgress!.CanComplete.Should().BeTrue();
    }

    [Fact]
    public void Assemble_Running_without_matches_allows_Complete()
    {
        var situation = TestCompetition.Create("Lifecycle Empty Running", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
        hub.Lifecycle.MatchProgress!.Total.Should().Be(0);
        hub.Lifecycle.MatchProgress.CanComplete.Should().BeTrue();
    }

    [Fact]
    public void Assemble_competition_Running_freezes_lone_Draft_phase_Prepare()
    {
        var competition = Competition.Create(new CompetitionName("Running multi"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        competition.AddEntry(TeamId.New(), "C", _clock);
        competition.AddEntry(TeamId.New(), "D", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        foreach (var entry in competition.Entries)
        {
            stage.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        competition.Prepare(_clock);
        competition.Start(_clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledConstructionFrozen);
    }

    [Fact]
    public void Assemble_competition_Running_freezes_lone_Ready_phase_Start()
    {
        var competition = Competition.Create(new CompetitionName("Frozen start"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        stage.Prepare(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledConstructionFrozen);
    }

    [Fact]
    public void Assemble_flux_qualif_wired_quarters_prepare_without_composition_entries()
    {
        var situation = TestCompetition.Create("Flux QF prepare", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4, "Phase de groupes"));
        var groups = situation.PrimaryStage!;
        var entries = situation.Competition.Entries.ToList();
        var orderedGroups = groups.Groups.OrderBy(group => group.Name, StringComparer.Ordinal).ToArray();
        for (var i = 0; i < entries.Count; i++)
        {
            groups.AssignEntryToGroup(orderedGroups[i / 4].Id, entries[i].Id);
        }

        var slotKeys = new[] { "QF1-A", "QF1-B", "QF2-A", "QF2-B" };
        var quarter = situation.AddKnockoutStage(
            "Quarts de finale",
            "Quarts de finale",
            slotKeys);

        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    2,
                    quarter.Id.Value,
                    DestinationSlotKeys: slotKeys)
            ],
            _clock);

        quarter.CompositionEntries.Should().BeEmpty();

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var qfHub = view.Stages.Single(h => h.StageId == quarter.Id.Value);

        qfHub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        qfHub.Lifecycle.Transition.Enabled.Should().BeTrue();

        PrepareStage.Execute(quarter, situation.Stages, _clock);
        quarter.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Assemble_flux_like_clean_primary_and_empty_ko_allow_Prepare_while_integrity_cycle_blocks()
    {
        var situation = TestCompetition.Create("Flux split prepare", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "Phase propre"));
        var clean = situation.PrimaryStage!;
        foreach (var entry in situation.Competition.Entries)
        {
            clean.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        var slotKeys = new[] { "QF1-A", "QF1-B", "QF2-A", "QF2-B" };
        var qf = situation.AddKnockoutStage(
            "Quarts de finale",
            "Quarts de finale",
            slotKeys);

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var cleanHub = view.Stages.Single(h => h.StageId == clean.Id.Value);
        var qfHub = view.Stages.Single(h => h.StageId == qf.Id.Value);

        cleanHub.Lifecycle!.Transition.Enabled.Should().BeTrue();
        qfHub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        qfHub.Lifecycle.Transition.Enabled.Should().BeTrue();
        qfHub.StructureIssues.Should().Contain(CompetitionFlowAnalyzer.SlotFeedMissing);

        ReplaceStageQualificationRules.Execute(
            clean,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.Overall,
                    1,
                    4,
                    qf.Id.Value,
                    DestinationSlotKeys: slotKeys)
            ],
            _clock);
        ReplaceStageQualificationRules.Execute(
            qf,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.Overall,
                    1,
                    1,
                    clean.Id.Value,
                    DestinationForm: true)
            ],
            _clock);

        view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        cleanHub = view.Stages.Single(h => h.StageId == clean.Id.Value);
        qfHub = view.Stages.Single(h => h.StageId == qf.Id.Value);

        cleanHub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        cleanHub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledStructureGraphInvalid);
        cleanHub.StructureIssues.Should().Contain(CompetitionFlowAnalyzer.CycleDetected);
        qfHub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        qfHub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledStructureGraphInvalid);
        qfHub.StructureIssues.Should().Contain(CompetitionFlowAnalyzer.CycleDetected);
    }

    [Fact]
    public void Assemble_competition_Running_allows_Prepare_on_downstream_Draft_when_peer_is_Running()
    {
        var situation = TestCompetition.Create("Downstream prepare", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "Phase aller"));
        var semi = situation.AddKnockoutStage(
            "Demi-finale",
            "Demi-finales",
            ["SF1-A", "SF1-B", "SF2-A", "SF2-B"]);
        foreach (var entry in situation.Competition.Entries)
        {
            semi.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        situation
            .PreparePrimaryStage()
            .StartPrimaryStage()
            .PrepareCompetition()
            .StartCompetition();

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var sfHub = view.Stages.Single(h => h.StageId == semi.Id.Value);

        sfHub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        sfHub.Lifecycle.Transition.DisabledReasonCode.Should().NotBe(
            StructureStageLifecycleAssembler.DisabledConstructionFrozen);
    }

    [Fact]
    public void Assemble_Running_projects_cancel_start_when_all_matches_scheduled()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Scheduled);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.CancelStartTransition.Should().NotBeNull();
        hub.Lifecycle.CancelStartTransition!.Code.Should()
            .Be(OverviewAssembler.ActionRevertToReadyStage);
        hub.Lifecycle.CancelStartTransition.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Assemble_Running_projects_cancel_start_disabled_when_a_match_is_finished()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Finished);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.CancelStartTransition.Should().NotBeNull();
        hub.Lifecycle.CancelStartTransition!.Enabled.Should().BeFalse();
        hub.Lifecycle.CancelStartTransition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledMatchLifecycleStarted);
    }

    [Fact]
    public void Assemble_Running_projects_Suspend_enabled_when_no_live_match()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Scheduled);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.RevertTransition.Should().NotBeNull();
        hub.Lifecycle.RevertTransition!.Code.Should().Be(OverviewAssembler.ActionSuspendStage);
        hub.Lifecycle.RevertTransition.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Assemble_Running_projects_Suspend_disabled_when_a_match_is_live()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Scheduled);
        match.Start(_clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.RevertTransition!.Code.Should().Be(OverviewAssembler.ActionSuspendStage);
        hub.Lifecycle.RevertTransition.Enabled.Should().BeFalse();
        hub.Lifecycle.RevertTransition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledMatchLiveInProgress);
    }

    [Fact]
    public void Assemble_Suspended_projects_Resume_enabled()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Scheduled);
        stage.Suspend(_clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.RevertTransition!.Code.Should().Be(OverviewAssembler.ActionResumeStage);
        hub.Lifecycle.RevertTransition.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Assemble_Suspended_projects_cancel_start_when_not_sportively_engaged()
    {
        var (competition, stage, match) = CreateRunningWithMatch(MatchStatus.Scheduled);
        stage.Suspend(_clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [ToSummaryRow(match)]
        };
        var view = StructureViewAssembler.Assemble(competition, [stage], matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.CancelStartTransition.Should().NotBeNull();
        hub.Lifecycle.CancelStartTransition!.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Assemble_Ready_projects_RevertToDraft_when_no_attached_matches()
    {
        var situation = TestCompetition.Create("Revert draft", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.RevertTransition.Should().NotBeNull();
        hub.Lifecycle.RevertTransition!.Code.Should().Be(OverviewAssembler.ActionRevertToDraftStage);
        hub.Lifecycle.RevertTransition.Enabled.Should().BeTrue();
    }

    /// <summary>
    /// Dangling qualification destination is visible on the read model and blocks Prepare before POST.
    /// </summary>
    [Fact]
    public void Assemble_Draft_prepare_disabled_when_qualification_targets_missing_stage()
    {
        var competition = Competition.Create(new CompetitionName("Outbound gap"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var source = Stage.Create(competition.Id, new StageName("Poules"), SampleRegulations.Standard(), _clock);
        competition.AddStage(source.Id, _clock);
        var group = source.AddGroup("A", _clock);
        source.AddMatchday(1, _clock);
        foreach (var entry in competition.Entries)
        {
            source.AssignEntryToGroup(group.Id, entry.Id);
        }

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(StageId.New()))
            ]),
            _clock);

        var act = () => PrepareStage.Execute(source, [source], _clock);
        act.Should().Throw<ApplicationFailureException>();

        var view = StructureViewAssembler.Assemble(competition, [source]);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledStructureGraphInvalid);
    }

    [Fact]
    public void Assemble_Ready_cup_without_slots_or_matches_blocks_Start_with_no_determined_reason()
    {
        var situation = TestCompetition.Create("Cup empty bracket", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        situation.PreparePrimaryStage();

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should().BeOneOf(
            StructureStageLifecycleAssembler.DisabledIncompleteEntryRound,
            StructureStageLifecycleAssembler.DisabledNoDeterminedConfrontations);
        hub.Materialize!.DisabledReason.Should()
            .Be(StructureMaterializeDisabledReasons.NoDeterminedConfrontations);
    }

    [Fact]
    public void Assemble_Ready_cup_slotted_non_playable_initial_round_blocks_Start_with_placement_remediation()
    {
        var situation = TestCompetition.Create("Cup initial round not playable", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        var round = stage.Rounds[0];
        stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        stage.AddFixture(round.Id, _clock, "S3", "S4", "P2");
        situation.PreparePrimaryStage();

        stage.Status.Should().Be(StageStatus.Ready);

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle.Should().NotBeNull();
        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);

        hub.Lifecycle.Transition.Remediation.Should().NotBeNull();
        hub.Lifecycle.Transition.Remediation!.Href.Should().Contain("focus=operational-placement");
        hub.Lifecycle.Transition.Remediation.Href.Should().Contain("manual=1");
        hub.Lifecycle.Transition.Remediation.Href.Should().NotContain("section=matchs");

        hub.Materialize.Should().NotBeNull();
        hub.Materialize!.CommandEnabled.Should().BeFalse();
        hub.Materialize.EligibleCount.Should().Be(0);
        hub.Materialize.DeterminedCount.Should().Be(0);
        hub.Materialize.MaterializedCount.Should().Be(0, "fixture shells without attached legs are not materialized confrontations");
        hub.Materialize.DisabledReason.Should()
            .Be(StructureMaterializeDisabledReasons.NoDeterminedConfrontations);
    }

    private static MatchSummaryRow ToSummaryRow(Match match) =>
        new(
            match.Id,
            match.StageId,
            match.Status,
            match.HomeEntryId,
            match.AwayEntryId,
            match.Result,
            match.HasObservedLive);

    private (Competition Competition, Stage Stage, Match Match) CreateRunningWithMatch(MatchStatus status)
    {
        var competition = Competition.Create(new CompetitionName("Running"), SampleRegulations.Standard(), _clock);
        var home = competition.AddEntry(TeamId.New(), "A", _clock);
        var away = competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("MD"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        PrepareStage.Execute(stage, [stage], _clock);
        StartStage.Execute(stage, _clock);
        var match = Match.Create(competition.Id, stage.Id, home.Id, away.Id, _clock);
        if (status != MatchStatus.Finished) return (competition, stage, match);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        return (competition, stage, match);
    }
}
