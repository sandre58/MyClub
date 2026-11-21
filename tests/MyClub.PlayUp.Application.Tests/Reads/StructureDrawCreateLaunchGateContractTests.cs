// -----------------------------------------------------------------------
// <copyright file="StructureDrawCreateLaunchGateContractTests.cs" company="Stéphane ANDRE">
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
/// Hub projection of <see cref="StructureDrawCreateLaunchGate"/> (see
/// docs/design/structure-lifecycle/structure-draw-create-launch-gate.md).
/// </summary>
public sealed class StructureDrawCreateLaunchGateContractTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Precursor_ToLaunch_badge_when_DrawRules_engaged_and_no_active_draw()
    {
        var situation = TestCompetition.Create("Cup launch badge", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);

        var badge = StructureViewAssembler.GetDrawExecutionBadge(stage, hasDrawRules: true);

        badge.Should().Be(StructureDrawExecutionBadge.ToLaunch);
    }

    [Fact]
    public void Precursor_InProgress_badge_after_CreateDraw_blocks_first_launch_window()
    {
        var situation = TestCompetition.Create("Cup draft draw", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        var badge = StructureViewAssembler.GetDrawExecutionBadge(stage, hasDrawRules: true);

        badge.Should().Be(StructureDrawExecutionBadge.InProgress);

        // Contract: DrawCreateLaunchAllowed == false (an active non-cancelled draw exists).
    }

    [Fact]
    public void Precursor_slot_grid_with_one_free_place_allows_partial_launch()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([e1.Id, e2.Id], _clock);
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, e1.Id, _clock);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Slot);

        facts.BlockingPlaceCount.Should().BeGreaterThan(0);
        facts.VacantForDrawLaunchCount.Should().Be(1);
        StructureViewAssembler.GetDrawExecutionBadge(stage, hasDrawRules: true)
            .Should().Be(StructureDrawExecutionBadge.ToLaunch);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.DrawCreateLaunch.Allowed.Should().BeTrue();
        hub.DrawCreateLaunch.BlockReasonCode.Should().BeNull();
    }

    [Fact]
    public void Slot_launch_does_not_count_direct_assignment_as_a_free_place()
    {
        var (competition, stage) = CupWithPopulation(2, 2);
        stage.AssignEntryToSlot(stage.Slots[0].SlotKey, stage.CompositionEntries[0].EntryId);

        var scope = SlotPartialDrawPlanner.Compose(stage, [stage]);
        scope.VacantSlotKeys.Should().Equal(stage.Slots[1].SlotKey);
        scope.UnplacedEntries.Should().ContainSingle(entry =>
            entry.Equals(stage.CompositionEntries[1].EntryId));

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.DrawCreateLaunch.Allowed.Should().BeTrue();
    }

    [Fact]
    public void Slot_launch_refuses_when_no_place_is_free()
    {
        var (competition, stage) = CupWithPopulation(2, 2);
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, stage.CompositionEntries[0].EntryId, _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, stage.CompositionEntries[1].EntryId, _clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();

        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockNoFreeSlot);
    }

    [Fact]
    public void Withdrawn_occupant_on_a_full_phase_is_not_a_free_place()
    {
        var (competition, stage) = CupWithPopulation(4, 4);
        var withdrawnKey = stage.Slots[0].SlotKey;
        var withdrawnEntry = stage.CompositionEntries[0].EntryId;
        for (var index = 0; index < stage.Slots.Count; index++)
        {
            stage.ApplyResolvedEntry(
                stage.Slots[index].SlotKey,
                stage.CompositionEntries[index].EntryId,
                _clock);
        }

        stage.RemoveCompositionEntryIfPresent(withdrawnEntry, _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, [stage]);
        SlotPartialDraw.Classify(stage, snapshot)
            .Single(fact => fact.SlotKey == withdrawnKey).Occupancy
            .Should().Be(SlotDrawPlaceOccupancy.Withdrawn);
        var scope = SlotPartialDraw.Compose(stage, snapshot);
        scope.Refusal.Should().Be(SlotPartialDrawRefusal.NoFreeSlot);
        scope.VacantSlotKeys.Should().BeEmpty();
        scope.VacantSlotKeys.Should().NotContain(withdrawnKey);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Slot);
        facts.VacantForDrawLaunchCount.Should().Be(0);
        facts.OccupiedByEntryCount.Should().Be(stage.Slots.Count);
        facts.BlockingPlaceCount.Should().Be(stage.Slots.Count);

        var metrics = StageOverviewAssembler.Assemble(stage, competition).SlotDrawDisplayMetrics!;
        metrics.VacantSlotKeyCount.Should().Be(0);
        metrics.VacantSlotKeys.Should().NotContain(withdrawnKey);
        metrics.UnplacedEntryCount.Should().Be(0);
        metrics.AlreadyPlacedEntryCount.Should().Be(3);
        metrics.EntryCount.Should().Be(3);
        metrics.PlaceCount.Should().Be(4);

        // Launch is refused here. Generate is not called, so this is not a later 409 DrawNoFreeSlot.
        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();
        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockNoFreeSlot);
    }

    [Fact]
    public void Slot_launch_refuses_when_unplaced_teams_do_not_fill_free_places()
    {
        var (competition, stage) = CupWithPopulation(4, 2);
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, stage.CompositionEntries[0].EntryId, _clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();

        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockNotEnoughTeams);
    }

    [Fact]
    public void Slot_launch_refuses_when_unplaced_teams_exceed_free_places()
    {
        var (competition, stage) = CupWithPopulation(2, 3);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();

        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockTooManyTeams);
    }

    [Fact]
    public void Groups_with_two_legal_occupants_allows_launch()
    {
        var situation = TestCompetition.Create("Groups occupied", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        var ids = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ReplaceAffectationAuthoring(ids, _clock);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
        stage.AssignEntryToGroup(stage.Groups[0].Id, ids[2]);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.DrawCreateLaunch.Allowed.Should().BeTrue();
        hub.DrawCreateLaunch.BlockReasonCode.Should().BeNull();
        hub.DrawCreateLaunch.BlockDetail.Should().BeNull();
    }

    [Theory]
    [InlineData("missing-pots")]
    [InlineData("shape")]
    [InlineData("inbound")]
    [InlineData("pot-conflict")]
    [InlineData("outside-composition")]
    [InlineData("capacity")]
    [InlineData("active-draw")]
    public void Groups_gate_reason_matches_the_server_code(string cause)
    {
        var (competition, stage, stages, intent) = BuildGroupsCause(cause);
        var hub = StructureViewAssembler.Assemble(competition, stages).Stages
            .Single(item => item.StageId == stage.Id.Value);
        var badge = cause == "active-draw"
            ? StructureDrawExecutionBadge.ToLaunch
            : hub.DrawExecutionBadge;
        var gateCode = StructureDrawCreateLaunchGate.Evaluate(
            competition,
            stage,
            stages,
            hub,
            badge,
            matchSummaries: null).BlockReasonCode;
        gateCode.Should().NotBeNull();

        var serverCode = RefusalCode(() => CreateDraw.Execute(
            stage,
            DrawResolutionKind.Group,
            _clock,
            competitionStages: stages,
            intent: intent));

        serverCode.Should().Be(gateCode switch
        {
            StructureDrawCreateLaunchGate.BlockMissingPots => ApplicationErrorCodes.DrawPotsMissing,
            StructureDrawCreateLaunchGate.BlockGroupShapeInvalid => ApplicationErrorCodes.DrawGroupShapeInvalid,
            StructureDrawCreateLaunchGate.BlockActiveDrawPresent => StageErrorCodes.NonCancelledDrawAlreadyExists,
            _ => gateCode
        });
        if (cause == "inbound")
        {
            serverCode.Should().Be(ApplicationErrorCodes.DrawGroupSeatsReservedByInboundFeed);
        }

        if (cause != "active-draw")
        {
            stage.Draws.Should().BeEmpty();
        }

        static string? RefusalCode(Action act)
        {
            try
            {
                act();
                return null;
            }
            catch (ApplicationFailureException exception)
            {
                return exception.Code;
            }
            catch (DomainException exception)
            {
                return exception.Code;
            }
        }
    }

    [Fact]
    public void Precursor_slot_vacant_grid_with_full_pool_and_no_direct_assignment_allows_launch_layer_L6()
    {
        var situation = TestCompetition.Create("Cup vacant grid", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);

        stage.DirectAssignments.Should().BeEmpty();
        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Slot);
        facts.BlockingPlaceCount.Should().Be(0);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();
        hub.CompositionEntryCount.Should().Be(4);
        hub.CompositionCapacity.Should().Be(4);
        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.ToLaunch);
        hub.DrawCreateLaunch.Allowed.Should().BeTrue();
        hub.DrawCreateLaunch.BlockReasonCode.Should().BeNull();
    }

    [Fact]
    public void Precursor_groups_shape_requires_pots_and_pool_product_on_hub()
    {
        var situation = TestCompetition.Create("Groups draw shape", _clock)
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

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();
        hub.NumberOfPots.Should().Be(2);
        hub.GroupCount.Should().Be(2);
        hub.CompositionEntryCount.Should().Be(4);
        hub.CompositionEntryCount.Should().Be(hub.NumberOfPots * hub.GroupCount);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Group);
        facts.BlockingPlaceCount.Should().Be(0);
    }

    [Fact]
    public void Precursor_championship_without_draw_rules_has_no_execution_badge()
    {
        var situation = TestCompetition.Create("Champ no draw", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"));

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();
        hub.HasDrawRules.Should().BeFalse();
        hub.DrawExecutionBadge.Should().BeNull();
        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockDrawRulesMissing);
    }

    [Fact]
    public void Hub_exposes_group_shape_block_when_pool_does_not_match_pots_times_groups()
    {
        var situation = TestCompetition.Create("Groups shape block", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Groups(2, 2));
        var stage = situation.PrimaryStage!;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring(
            [.. situation.Competition.Entries.Select(entry => entry.Id)],
            _clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.DrawCreateLaunch.Allowed.Should().BeFalse();
        hub.DrawCreateLaunch.BlockReasonCode.Should().Be(StructureDrawCreateLaunchGate.BlockGroupShapeInvalid);
    }

    private (Competition Competition, Stage Stage, Stage[] Stages, DrawInputsIntent Intent) BuildGroupsCause(
        string cause)
    {
        var competition = CreateCompetition.Execute(cause, _clock);
        var count = cause == "shape" ? 3 : 4;
        var ids = Enumerable.Range(0, count)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var places = cause == "capacity" ? 4 : 2;
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, places), _clock).Stage;
        if (cause == "missing-pots")
        {
            ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        }
        else
        {
            var pots = cause == "capacity" ? 4 : 2;
            ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random, potRules: new PotRules(pots)), _clock);
        }

        stage.ReplaceAffectationAuthoring(ids, _clock);
        var stages = new List<Stage> { stage };
        var intent = DrawInputsIntent.Default;
        switch (cause)
        {
            case "inbound":
                stage.ReplaceAffectationAuthoring(ids.Take(3).ToArray(), _clock);
                var source = Stage.Create(competition.Id, new StageName("Source"), SampleRegulations.Standard(), _clock);
                source.ReplaceQualificationRules(
                    new QualificationRules(
                    [
                        new QualificationPath(
                            1,
                            QualificationSource.Overall(),
                            new QualificationSelection(SelectionMode.Position, 1),
                            QualificationDestination.ForGroup(stage.Id, stage.Groups[0].Id))
                    ]),
                    _clock);
                stages.Insert(0, source);
                break;
            case "pot-conflict":
                stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
                stage.AssignEntryToGroup(stage.Groups[0].Id, ids[1]);
                break;
            case "outside-composition":
                stage.AssignEntryToGroup(stage.Groups[0].Id, EntryId.New());
                break;
            case "capacity":
                stage.AssignEntryToGroup(stage.Groups[0].Id, ids[0]);
                stage.AssignEntryToGroup(stage.Groups[0].Id, ids[1]);
                stage.AssignEntryToGroup(stage.Groups[0].Id, ids[2]);
                break;
            case "active-draw":
                CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock, competitionStages: [stage]);
                break;
        }

        return (competition, stage, [.. stages], intent);
    }

    private (Competition Competition, Stage Stage) CupWithPopulation(int slots, int teams)
    {
        var competition = CreateCompetition.Execute($"Cup {slots}x{teams}", _clock);
        var ids = new List<EntryId>();
        for (var index = 0; index < teams; index++)
        {
            ids.Add(AddEntry.Execute(competition, $"T{index}", _clock).Id);
        }

        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(slots), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(ids, _clock);
        return (competition, stage);
    }
}
