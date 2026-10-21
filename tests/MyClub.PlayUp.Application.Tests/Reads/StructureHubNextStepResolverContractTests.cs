// -----------------------------------------------------------------------
// <copyright file="StructureHubNextStepResolverContractTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Product contract for <see cref="StructureHubNextStepResolver"/> (S1–S14, C1–C8, Q1, G1).
/// </summary>
public sealed class StructureHubNextStepResolverContractTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void S1_single_draft_prepare_enabled()
    {
        var situation = TestCompetition.Create("S1", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"));

        var step = Resolve(situation);

        step.Should().NotBeNull();
        step.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        step.StageName.Should().Be("League");
    }

    [Fact]
    public void S2_two_draft_phases_both_prepare_enabled_returns_null()
    {
        var (competition, stages) = CreateTwoDraftChampionshipPhases("Phase A", "Phase B");

        Decide(competition, stages).Should().BeNull();
    }

    [Fact]
    public void S3_suspended_resume_enabled()
    {
        var situation = TestCompetition.Create("S3", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        situation.PrimaryStage!.Suspend(situation.Clock);

        var step = Resolve(situation);

        step.Should().NotBeNull();
        step.Code.Should().Be(OverviewAssembler.ActionResumeStage);
    }

    [Fact]
    public void S4_run_draw_when_launch_gate_ok_not_badge_only()
    {
        var situation = TestCompetition.Create("S4", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);
        PrepareStage.Execute(stage, situation.Stages, _clock);

        var step = Resolve(situation);

        step.Should().NotBeNull();
        step.Code.Should().Be(StructureViewAssembler.NextStepRunDraw);
    }

    [Fact]
    public void S5_apply_draw_when_authorized()
    {
        var situation = TestCompetition.Create("S5", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);
        PrepareStage.Execute(stage, situation.Stages, _clock);
        PublishResolvedGroupDraw(stage);

        var step = Resolve(situation);

        step.Should().NotBeNull();
        step.Code.Should().Be(OverviewAssembler.ActionApplyDraw);
    }

    [Fact]
    public void S6_materialize_when_command_enabled()
    {
        var situation = TestCompetition.Create("S6", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();

        var step = Resolve(situation);

        step.Should().NotBeNull();
        step.Code.Should().Be(OverviewAssembler.ActionMaterializeMatches);
    }

    [Fact]
    public void S7_complete_when_no_higher_priority_construction_on_running_stage()
    {
        var situation = TestCompetition.Create("S7", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();

        var step = Resolve(situation);

        step.Should().NotBeNull();
        step.Code.Should().Be(OverviewAssembler.ActionCompleteStage);
    }

    [Fact]
    public void S9_calm_structure_resolver_null_when_phase_completed_and_no_other_work()
    {
        var situation = TestCompetition.Create("S9", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        situation.PrimaryStage!.Complete(situation.Clock);

        Resolve(situation).Should().BeNull();
    }

    [Fact]
    public void S13_never_emits_schedule_or_swiss_hub_step_codes()
    {
        var situation = TestCompetition.Create("S13", _clock)
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        foreach (var hint in new[]
                 {
                     StructureViewAssembler.NextStepBuildSchedule,
                     StructureViewAssembler.NextStepRunSwissRounds
                 })
        {
            var hinted = view with
            {
                Readiness = view.Readiness with
                {
                    ReadyForNextSlice = true,
                    NextConstructionStepHint = hint
                }
            };

            var step = StructureHubNextStepResolver.Decide(
                situation.Competition,
                situation.Stages,
                hinted);
            step?.Code.Should().NotBe(hint);
        }
    }

    [Fact]
    public void S11_completed_competition_resolver_null()
    {
        var situation = TestCompetition.Create("S11", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage()
            .StartPrimaryStage();
        situation.PrimaryStage!.Complete(_clock);
        situation.Competition.Prepare(situation.Clock);
        situation.Competition.Start(situation.Clock);
        situation.Competition.Complete(CompletionMode.Normal, situation.Clock);

        Resolve(situation).Should().BeNull();
    }

    [Fact]
    public void S12_prepare_disabled_never_emits_prepare()
    {
        var competition = Competition.Create(new CompetitionName("Frozen"), SampleRegulations.Standard(), _clock);
        competition.AddEntry(TeamId.New(), "A", _clock);
        competition.AddEntry(TeamId.New(), "B", _clock);
        var stage = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        stage.AddMatchday(1, _clock);
        competition.AddStage(stage.Id, _clock);
        competition.Prepare(_clock);
        competition.Start(_clock);

        Decide(competition, [stage]).Should().BeNull();
    }

    [Fact]
    public void C1_materialize_before_start_on_same_ready_phase()
    {
        var situation = TestCompetition.Create("C1", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"))
            .PreparePrimaryStage();

        Resolve(situation)!.Code.Should().Be(OverviewAssembler.ActionMaterializeMatches);
    }

    [Fact]
    public void C2_prepare_before_run_draw_on_draft_phase()
    {
        var situation = TestCompetition.Create("C2", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);

        Resolve(situation)!.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
    }

    [Fact]
    public void C3_apply_before_run_on_same_stage()
    {
        var situation = TestCompetition.Create("C3", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);
        PrepareStage.Execute(stage, situation.Stages, _clock);
        PublishResolvedGroupDraw(stage);

        Resolve(situation)!.Code.Should().Be(OverviewAssembler.ActionApplyDraw);
    }

    [Fact]
    public void C5_two_stages_both_apply_authorized_returns_null()
    {
        var (competition, stages) = CreateTwoGroupPhasesWithPublishedDraws();

        Decide(competition, stages).Should().BeNull();
    }

    [Fact]
    public void C6_materialize_before_complete_across_stages()
    {
        var (competition, stages) = CreateReadyMaterializeAndRunningCompletePhases();

        Decide(competition, stages)!.Code.Should().Be(OverviewAssembler.ActionMaterializeMatches);
    }

    [Fact]
    public void C7_run_draw_before_start_when_draw_required_on_ready_cup()
    {
        var situation = TestCompetition.Create("C7", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);
        PrepareStage.Execute(stage, situation.Stages, _clock);

        Resolve(situation)!.Code.Should().Be(StructureViewAssembler.NextStepRunDraw);
    }

    [Fact]
    public void C8_to_launch_with_blocking_places_does_not_emit_run_draw()
    {
        var situation = TestCompetition.Create("C8", _clock)
            .WithTeams("A", "B")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        var entry = situation.Competition.Entries[0];
        stage.ReplaceAffectationAuthoring([entry.Id], _clock);
        stage.AssignEntryToSlot(stage.Slots[0].SlotKey, entry.Id);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();
        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.ToLaunch);
        StructureDrawCreateLaunchGate.Evaluate(
                situation.Competition,
                stage,
                situation.Stages,
                hub,
                hub.DrawExecutionBadge,
                null)
            .Allowed.Should().BeFalse();

        Resolve(situation)!.Code.Should().NotBe(StructureViewAssembler.NextStepRunDraw);
    }

    [Fact]
    public void Q1_lower_tier_wins_across_stages_without_using_stage_order()
    {
        var (competition, stages) = CreateReadyMaterializeAndRunningCompletePhases();

        var forward = Decide(competition, stages);
        var reverse = Decide(competition, [.. stages.AsEnumerable().Reverse()]);

        forward.Should().NotBeNull();
        forward.Code.Should().Be(OverviewAssembler.ActionMaterializeMatches);
        reverse.Should().BeEquivalentTo(forward);
    }

    [Fact]
    public void G1_upstream_empty_composition_pool_blocks_run_draw()
    {
        var (competition, stages) = CreateQualGroupsAndKnockoutDraft();
        var knockout = stages[1];
        var hub = StructureViewAssembler.Assemble(competition, stages)
            .Stages.Single(stage => stage.StageId == knockout.Id.Value);
        hub.IsRootComposition.Should().BeFalse();
        hub.CompositionEntryCount.Should().Be(0);
        StructureDrawCreateLaunchGate.Evaluate(
                competition,
                knockout,
                stages,
                hub,
                StructureDrawExecutionBadge.ToLaunch,
                null)
            .Allowed.Should().BeFalse();

        StructureDrawCreateLaunchGate.Evaluate(
                competition,
                knockout,
                stages,
                hub,
                StructureDrawExecutionBadge.ToLaunch,
                null)
            .BlockReasonCode.Should()
            .Be(StructureDrawCreateLaunchGate.BlockUpstreamCompositionPoolEmpty);
        var hubStep = Decide(competition, stages);
        hubStep?.Code.Should().NotBe(StructureViewAssembler.NextStepRunDraw);
    }

    private static StructureHubNextStepDto? Resolve(TestCompetition situation) => Decide(situation.Competition, situation.Stages);

    private static StructureHubNextStepDto? Decide(Competition competition, IReadOnlyList<Stage> stages)
    {
        var view = StructureViewAssembler.Assemble(competition, stages);
        return StructureHubNextStepResolver.Decide(competition, stages, view);
    }

    private void PublishResolvedGroupDraw(Stage stage)
    {
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs);
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]);
        PublishDraw.Execute(stage, draw.Id, _clock);
    }

    private (Competition Competition, List<Stage> Stages) CreateTwoDraftChampionshipPhases(
        string nameA,
        string nameB)
    {
        var competition = Competition.Create(new CompetitionName("Two draft"), SampleRegulations.Standard(), _clock);
        foreach (var label in new[] { "A", "B", "C", "D" })
        {
            AddEntry.Execute(competition, label, _clock);
        }

        var stageA = Stage.Create(competition.Id, new StageName(nameA), SampleRegulations.Standard(), _clock);
        stageA.AddMatchday(1, _clock);
        competition.AddStage(stageA.Id, _clock);
        foreach (var entry in competition.Entries)
        {
            stageA.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        var stageB = Stage.Create(competition.Id, new StageName(nameB), SampleRegulations.Standard(), _clock);
        stageB.AddMatchday(1, _clock);
        competition.AddStage(stageB.Id, _clock);
        foreach (var entry in competition.Entries)
        {
            stageB.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        return (competition, [stageA, stageB]);
    }

    private (Competition Competition, List<Stage> Stages) CreateTwoGroupPhasesWithPublishedDraws()
    {
        var competition = Competition.Create(new CompetitionName("Two apply"), SampleRegulations.Standard(), _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var stageA = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stageA,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stageA.ReplaceAffectationAuthoring([.. competition.Entries.Select(entry => entry.Id)], _clock);
        PrepareStage.Execute(stageA, [stageA], _clock);
        PublishResolvedGroupDraw(stageA);

        var stageB = Stage.Create(competition.Id, new StageName("Groups B"), SampleRegulations.Standard(), _clock);
        stageB.AddGroup("G1", _clock);
        stageB.AddGroup("G2", _clock);
        stageB.AddMatchday(1, _clock);
        competition.AddStage(stageB.Id, _clock);
        ReplaceStageDrawRules.Execute(
            stageB,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stageB.ReplaceAffectationAuthoring([.. competition.Entries.Select(entry => entry.Id)], _clock);
        PrepareStage.Execute(stageB, [stageA, stageB], _clock);
        PublishResolvedGroupDraw(stageB);

        return (competition, [stageA, stageB]);
    }

    private (Competition Competition, List<Stage> Stages) CreateReadyMaterializeAndRunningCompletePhases()
    {
        var competition = Competition.Create(new CompetitionName("Mat vs complete"), SampleRegulations.Standard(), _clock);
        foreach (var label in new[] { "A", "B", "C", "D" })
        {
            AddEntry.Execute(competition, label, _clock);
        }

        var ready = Stage.Create(competition.Id, new StageName("Ready"), SampleRegulations.Standard(), _clock);
        ready.AddMatchday(1, _clock);
        competition.AddStage(ready.Id, _clock);
        foreach (var entry in competition.Entries)
        {
            ready.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        PrepareStage.Execute(ready, [ready], _clock);

        var running = Stage.Create(competition.Id, new StageName("Running"), SampleRegulations.Standard(), _clock);
        running.AddMatchday(1, _clock);
        competition.AddStage(running.Id, _clock);
        foreach (var entry in competition.Entries)
        {
            running.AddResolvedPopulationEntry(entry.Id, _clock);
        }

        PrepareStage.Execute(running, [ready, running], _clock);
        running.Start(_clock);

        return (competition, [ready, running]);
    }

    private (Competition Competition, List<Stage> Stages) CreateQualGroupsAndKnockoutDraft()
    {
        var situation = TestCompetition.Create("Flux", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4, "Groups"));
        var groups = situation.PrimaryStage!;
        var entries = situation.Competition.Entries.ToList();
        var orderedGroups = groups.Groups.OrderBy(group => group.Name, StringComparer.Ordinal).ToArray();
        for (var i = 0; i < entries.Count; i++)
        {
            groups.AssignEntryToGroup(orderedGroups[i / 4].Id, entries[i].Id);
        }

        groups.ReplaceAffectationAuthoring([.. entries.Select(entry => entry.Id)], situation.Clock);

        var slotKeys = new[] { "QF1-A", "QF1-B", "QF2-A", "QF2-B" };
        var knockout = situation.AddKnockoutStage("KO", "KO", slotKeys);
        ReplaceStageDrawRules.Execute(knockout, new DrawRules(DrawMode.Random), _clock);
        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    2,
                    knockout.Id.Value,
                    DestinationSlotKeys: slotKeys)
            ],
            situation.Clock);

        return (situation.Competition, [groups, knockout]);
    }
}
