// -----------------------------------------------------------------------
// <copyright file="GenerateDrawResolutionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class GenerateDrawResolutionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 12, 14, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void Execute_slot_records_resolved_resolution()
    {
        var stage = CreateStage();
        var entries = NewEntries(4);
        foreach (var key in new[] { "QF1", "QF2", "QF3", "QF4" })
        {
            stage.AddSlot(key);
        }

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(entries));

        var result = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: ["QF1", "QF2", "QF3", "QF4"],
            seed: 42);

        result.IsResolved.Should().BeTrue();
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        draw.Resolution.SlotResults.Should().HaveCount(4);
        draw.Status.Should().Be(DrawStatus.Draft);
    }

    [Fact]
    public void Execute_same_seed_is_reproducible_via_application()
    {
        var stage1 = CreateStage();
        var stage2 = CreateStage();
        var entries = NewEntries(4);
        var targets = new[] { "A", "B", "C", "D" };
        var draw1 = PrepareSlotDraw(stage1, entries, targets);
        var draw2 = PrepareSlotDraw(stage2, entries, targets);

        var r1 = GenerateDrawResolution.Execute(stage1, draw1.Id, _clock, targets, seed: 123);
        var r2 = GenerateDrawResolution.Execute(stage2, draw2.Id, _clock, targets, seed: 123);

        r1.Resolution!.SlotResults.Select(s => (s.SlotKey, s.EntryId.Value))
            .Should().Equal(r2.Resolution!.SlotResults.Select(s => (s.SlotKey, s.EntryId.Value)));
    }

    [Fact]
    public void Execute_rejects_published_draw()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);

        var act = () => GenerateDrawResolution.Execute(stage, draw.Id, _clock, ["A"], seed: 1);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawGenerationFailure);
    }

    [Fact]
    public void Execute_invalid_request_does_not_mark_no_solution()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        stage.AddSlot("B");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(NewEntries(2)));

        // Targets exist on Stage but coverage is wrong → Domain Invalid (not Application preflight).
        var act = () => GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: ["A"],
            seed: 1);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
        draw.Resolution.State.Should().Be(DrawResolutionState.NotResolved);
    }

    [Fact]
    public void Execute_rejects_slot_target_missing_on_stage()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(NewEntries(2)));

        var act = () => GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: ["A", "Missing"],
            seed: 1);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawGenerationFailure);
        draw.Resolution.State.Should().Be(DrawResolutionState.NotResolved);
    }

    [Fact]
    public void Execute_slot_with_fixed_preserves_fixed_placements()
    {
        var stage = CreateStage();
        var entries = NewEntries(4);
        foreach (var key in new[] { "S1", "S2", "S3", "S4" })
        {
            stage.AddSlot(key);
        }

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForSlot(
                entries,
                fixedPlacements:
                [
                    new SlotDrawPlacement(entries[0], "S1"),
                    new SlotDrawPlacement(entries[1], "S2")
                ]));

        var result = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: ["S1", "S2", "S3", "S4"],
            seed: 9);

        result.IsResolved.Should().BeTrue();
        draw.Resolution.SlotResults.Should().Contain(s => s.EntryId.Equals(entries[0]) && s.SlotKey == "S1");
        draw.Resolution.SlotResults.Should().Contain(s => s.EntryId.Equals(entries[1]) && s.SlotKey == "S2");
    }

    [Fact]
    public void Execute_group_records_resolved_resolution()
    {
        var stage = CreateStageWithPots(numberOfPots: 4);
        var entries = NewEntries(8);
        var groups = new[] { stage.AddGroup("A", _clock), stage.AddGroup("B", _clock) };
        var pots = BalancedPots(entries, 4);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForGroup(entries, potMembership: new PotMembership(pots)));

        var result = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. groups.Select(g => g.Id)],
            seed: 5);

        result.IsResolved.Should().BeTrue();
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
        draw.Resolution.GroupResults.Should().HaveCount(8);
    }

    [Fact]
    public void Execute_group_target_missing_on_stage_fails()
    {
        var stage = CreateStageWithPots(2);
        var entries = NewEntries(4);
        var existing = stage.AddGroup("A", _clock);
        var missing = GroupId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForGroup(entries, potMembership: new PotMembership(BalancedPots(entries, 2))));

        var act = () => GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [existing.Id, missing],
            seed: 1);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawGenerationFailure);
        draw.Resolution.State.Should().Be(DrawResolutionState.NotResolved);
    }

    [Fact]
    public void Execute_group_without_pot_rules_fails()
    {
        var stage = CreateStage();
        var g1 = stage.AddGroup("A", _clock);
        var g2 = stage.AddGroup("B", _clock);
        var entries = NewEntries(4);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForGroup(entries, potMembership: new PotMembership(BalancedPots(entries, 2))));

        var act = () => GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [g1.Id, g2.Id],
            seed: 1);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawGenerationFailure);
    }

    [Fact]
    public void Execute_group_missing_pot_membership_is_invalid()
    {
        var stage = CreateStageWithPots(2);
        var g1 = stage.AddGroup("A", _clock);
        var g2 = stage.AddGroup("B", _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup(NewEntries(4)));

        var act = () => GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [g1.Id, g2.Id],
            seed: 1);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
        draw.Resolution.State.Should().Be(DrawResolutionState.NotResolved);
    }

    [Fact]
    public void Execute_group_generate_publish_apply_assigns_entries_to_groups()
    {
        var stage = CreateStageWithPots(2);
        var entries = NewEntries(4);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var pots = BalancedPots(entries, 2);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForGroup(entries, potMembership: new PotMembership(pots)));

        var generated = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [groupA.Id, groupB.Id],
            seed: 11);

        generated.IsResolved.Should().BeTrue();
        stage.PublishDraw(draw.Id, _clock);

        var applied = ApplyDraw.Execute(stage, draw.Id, _clock);

        applied.CreatedMatches.Should().BeEmpty();
        groupA.EntryIds.Should().HaveCount(2);
        groupB.EntryIds.Should().HaveCount(2);
        groupA.EntryIds.Concat(groupB.EntryIds).Should().BeEquivalentTo(entries);
        pots[groupA.EntryIds[0]].Should().NotBe(pots[groupA.EntryIds[1]]);
        pots[groupB.EntryIds[0]].Should().NotBe(pots[groupB.EntryIds[1]]);
    }

    [Fact]
    public void Execute_group_passes_max_association_and_can_no_solution()
    {
        var stage = CreateStageWithPots(2);
        var entries = NewEntries(4);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var pots = BalancedPots(entries, 2);
        var association = AssociationId.New();
        var associations = entries.ToDictionary(e => e, _ => association);
        stage.ReplaceDrawRules(
            new DrawRules(
                DrawMode.Random,
                potRules: new PotRules(2),
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(1)]),
            _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForGroup(entries, potMembership: new PotMembership(pots)));

        var result = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [groupA.Id, groupB.Id],
            constraintContext: new DrawConstraintContext(null, null, associations),
            seed: 1);

        result.IsNoSolution.Should().BeTrue();
        draw.Resolution.State.Should().Be(DrawResolutionState.NoSolution);
    }

    [Fact]
    public void Execute_group_passes_max_association_and_resolves_when_feasible()
    {
        var stage = CreateStageWithPots(2);
        var entries = NewEntries(4);
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var pots = BalancedPots(entries, 2);
        var a1 = AssociationId.New();
        var a2 = AssociationId.New();
        var associations = new Dictionary<EntryId, AssociationId>
        {
            [entries[0]] = a1,
            [entries[1]] = a2,
            [entries[2]] = a1,
            [entries[3]] = a2
        };
        stage.ReplaceDrawRules(
            new DrawRules(
                DrawMode.Random,
                potRules: new PotRules(2),
                constraints: [DrawConstraint.MaxSameAssociationPerGroup(1)]),
            _clock);
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(
            draw.Id,
            DrawInputs.ForGroup(entries, potMembership: new PotMembership(pots)));

        var result = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [groupA.Id, groupB.Id],
            constraintContext: new DrawConstraintContext(null, null, associations),
            seed: 3);

        result.IsResolved.Should().BeTrue();
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    [Fact]
    public void Execute_filters_pairing_constraints_off_slot_and_resolves()
    {
        // S9: Application filters SameGroup off Slot; generation proceeds without Domain Invalid.
        var stage = CreateStage();
        stage.ReplaceDrawRules(
            new DrawRules(
                DrawMode.Random,
                constraints:
                [
                    new DrawConstraint(DrawConstraintType.SameGroupAvoidance, ConstraintEnforcement.Required)
                ]),
            _clock);
        foreach (var key in new[] { "A", "B" })
        {
            stage.AddSlot(key);
        }

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(NewEntries(2)));

        var result = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: ["A", "B"],
            seed: 1);

        result.IsResolved.Should().BeTrue();
        result.PreferredViolations.Should().BeEmpty();
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    private static Dictionary<EntryId, int> BalancedPots(EntryId[] entries, int numberOfPots)
    {
        var perPot = entries.Length / numberOfPots;
        var pots = new Dictionary<EntryId, int>();
        var index = 0;
        for (var pot = 1; pot <= numberOfPots; pot++)
        {
            for (var i = 0; i < perPot; i++)
            {
                pots[entries[index++]] = pot;
            }
        }

        return pots;
    }

    private static EntryId[] NewEntries(int count) =>
        [.. Enumerable.Range(0, count).Select(_ => EntryId.New())];

    private Stage CreateStageWithPots(int numberOfPots)
    {
        var stage = CreateStage();
        stage.ReplaceDrawRules(
            new DrawRules(DrawMode.Random, potRules: new PotRules(numberOfPots)),
            _clock);
        return stage;
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);

    private Draw PrepareSlotDraw(Stage stage, IReadOnlyList<EntryId> entries, IReadOnlyList<string> targets)
    {
        foreach (var key in targets)
        {
            stage.AddSlot(key);
        }

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(entries));
        return draw;
    }
}
