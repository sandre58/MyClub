// -----------------------------------------------------------------------
// <copyright file="StartStageReadinessTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class StartStageReadinessTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 12, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Start_blocked_when_championship_has_unmaterialized_eligible_confrontations()
    {
        var situation = TestCompetition.Create("Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(stage, situation.Competition, []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StartBlockedUnmaterialized);
    }

    [Fact]
    public void Start_allowed_when_championship_fully_materialized()
    {
        var situation = TestCompetition.Create("Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        var summaries = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches);

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(stage, situation.Competition, summaries);

        act.Should().NotThrow();
        StartStage.Execute(stage, _clock);
        stage.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Start_blocked_when_championship_roster_complete_but_no_confrontation_can_be_established()
    {
        var situation = TestCompetition.Create("Solo", _clock)
            .WithTeams("A")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();

        StageStartReadinessGuard.TryGetDisabledReasonCode(
                stage,
                situation.Competition,
                [],
                situation.Stages)
            .Should()
            .Be(StageStartReadinessGuard.DisabledNoExploitableInitialConfrontations);

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(stage, situation.Competition, []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StartBlockedNoExploitableInitialConfrontations);
    }

    [Fact]
    public void Read_Start_disabled_when_no_exploitable_initial_confrontations()
    {
        var situation = TestCompetition.Create("Solo", _clock)
            .WithTeams("A")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledNoExploitableInitialConfrontations);
        hub.Lifecycle.Transition.Remediation.Should().NotBeNull();
        hub.Lifecycle.Transition.Remediation!.Href.Should().Contain("focus=rail-entries");
    }

    [Fact]
    public void TryGetDisabledReasonCode_groups_ready_with_confrontations_returns_unmaterialized_not_no_exploitable()
    {
        var situation = TestCompetition.Create("Groups", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.PrimaryStage!;
        var entries = situation.Competition.Entries.OrderBy(entry => entry.Id.Value).ToArray();
        stage.AssignEntryToGroup(stage.Groups[0].Id, entries[0].Id);
        stage.AssignEntryToGroup(stage.Groups[0].Id, entries[1].Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, entries[2].Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, entries[3].Id);
        situation.PreparePrimaryStage();

        StageStartReadinessGuard.TryGetDisabledReasonCode(
                stage,
                situation.Competition,
                [],
                situation.Stages)
            .Should()
            .Be(StageStartReadinessGuard.DisabledUnmaterializedConfrontations);
    }

    [Fact]
    public void Start_blocked_when_groups_placement_capacity_not_met()
    {
        var competition = CreateCompetition.Execute("Groups partial pools", _clock);
        for (var i = 0; i < 8; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock);
        var stage = configured.Stage;
        var entries = competition.Entries.OrderBy(entry => entry.Id.Value).ToArray();
        stage.AssignEntryToGroup(stage.Groups[0].Id, entries[0].Id);
        stage.AssignEntryToGroup(stage.Groups[0].Id, entries[1].Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, entries[2].Id);
        stage.AssignEntryToGroup(stage.Groups[1].Id, entries[3].Id);
        StageMaterializationTestSupport.EnsureReadyForMaterialize(competition, stage, [stage], _clock);

        StageStartReadinessGuard.TryGetDisabledReasonCode(
                stage,
                competition,
                [],
                [stage])
            .Should()
            .Be(StageStartReadinessGuard.DisabledUnmaterializedConfrontations);

        var view = StructureViewAssembler.Assemble(competition, [stage]);
        var hub = view.Stages[0];
        hub.Lifecycle.Should().NotBeNull();
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
    }

    [Fact]
    public void Start_allowed_for_swiss_without_matches()
    {
        var situation = TestCompetition.Create("Swiss", _clock)
            .WithTeams(4)
            .WithStructure(StructureIntent.Swiss(roundCount: 3))
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(stage, situation.Competition, []);

        act.Should().NotThrow();
    }

    [Fact]
    public void Read_Start_disabled_when_unmaterialized_confrontations_remain()
    {
        var situation = TestCompetition.Create("Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionStartStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
    }

    [Fact]
    public void TryGetDisabledReasonCode_championship_roster_incomplete_returns_phase_roster_not_unmaterialized()
    {
        var competition = CreateCompetition.Execute("Qual-fed roster gap", _clock);
        var groups = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(groups.Id, _clock);
        _ = groups.AddGroup("A", _clock);
        _ = groups.AddGroup("B", _clock);
        groups.AddMatchday(1, _clock);

        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(champ.Id, _clock);
        var e1 = competition.AddEntry(TeamId.New(), "E1", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "E2", _clock).Id;
        champ.ReplaceAffectationAuthoring([e1, e2], _clock);

        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    2,
                    champ.Id.Value,
                    DestinationForm: true)
            ],
            _clock);

        PrepareStage.Execute(champ, [groups, champ], _clock);
        champ.Status.Should().Be(StageStatus.Ready);

        StageStartReadinessGuard.TryGetDisabledReasonCode(
                champ,
                competition,
                [],
                [groups, champ])
            .Should()
            .Be(StructureMaterializeDisabledReasons.PhaseRosterIncomplete);

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(
            champ,
            competition,
            [],
            [groups, champ]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StartBlockedPhaseRosterIncomplete);
    }

    [Fact]
    public void Read_Start_disabled_when_phase_roster_incomplete_not_unmaterialized()
    {
        var competition = CreateCompetition.Execute("Qual-fed roster gap read", _clock);
        var groups = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(groups.Id, _clock);
        _ = groups.AddGroup("A", _clock);
        _ = groups.AddGroup("B", _clock);
        groups.AddMatchday(1, _clock);

        var champ = Stage.Create(competition.Id, new StageName("Championnat"), SampleRegulations.Standard(), _clock);
        champ.AddMatchday(1, _clock);
        competition.AddStage(champ.Id, _clock);
        var e1 = competition.AddEntry(TeamId.New(), "E1", _clock).Id;
        var e2 = competition.AddEntry(TeamId.New(), "E2", _clock).Id;
        champ.ReplaceAffectationAuthoring([e1, e2], _clock);

        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    2,
                    champ.Id.Value,
                    DestinationForm: true)
            ],
            _clock);

        PrepareStage.Execute(champ, [groups, champ], _clock);

        var view = StructureViewAssembler.Assemble(competition, [groups, champ]);
        var hub = view.Stages.Single(stage => stage.StageId == champ.Id.Value);

        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledPhaseRosterIncomplete);
    }

    [Fact]
    public void Read_Start_enabled_when_championship_fully_materialized()
    {
        var situation = TestCompetition.Create("Ch", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches)
        };
        var view = StructureViewAssembler.Assemble(
            situation.Competition,
            situation.Stages,
            matchSummariesByStage: summaries);
        var hub = view.Stages.Single();

        hub.Lifecycle!.Transition.Enabled.Should().BeTrue();
    }

    [Fact]
    public void TryGetDisabledReasonCode_cup_ready_slotted_non_playable_initial_round()
    {
        var situation = TestCompetition.Create("Cup IRR guard", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        var round = stage.Rounds[0];
        stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        stage.AddFixture(round.Id, _clock, "S3", "S4", "P2");
        situation.PreparePrimaryStage();

        var reason = StageStartReadinessGuard.TryGetDisabledReasonCode(
            stage,
            situation.Competition,
            [],
            situation.Stages);

        reason.Should().Be(StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);
    }

    [Fact]
    public void Start_blocked_when_cup_canonical_entry_round_incomplete_after_eligible_materialized()
    {
        var situation = TestCompetition.Create("Cup partial R0", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        var a = EntryId.New();
        var b = EntryId.New();
        stage.ApplyResolvedEntry("S1", a, _clock);
        stage.ApplyResolvedEntry("S2", b, _clock);
        situation.PreparePrimaryStage();

        var materialized = MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            ["P1"],
            [],
            _clock);
        var summaries = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches);

        var act = () => StageStartReadinessGuard.EnsureReadyToStart(
            stage,
            situation.Competition,
            summaries);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StartBlockedIncompleteEntryRound);

        StageStartReadinessGuard.TryGetDisabledReasonCode(
                stage,
                situation.Competition,
                summaries,
                situation.Stages)
            .Should()
            .Be(StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);
    }
}
