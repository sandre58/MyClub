// -----------------------------------------------------------------------
// <copyright file="CupSlotPlacementGOps3B1Tests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

/// <summary>
/// G-OPS3 B1 — Cup <see cref="AssignEntryToSlot"/> / <see cref="ClearSlotAssignment"/> vs Applied draw.
/// </summary>
public sealed class CupSlotPlacementGOps3B1Tests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 15, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Clear_refuses_when_slot_protected_by_applied_draw()
    {
        var (stage, _, protectedSlot, _) = SeedCupWithAppliedSlotDraw();

        var act = () => ClearSlotAssignment.Execute(stage, protectedSlot);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawSlotPlacementProtected);
    }

    [Fact]
    public void Assign_refuses_when_changing_protected_entry()
    {
        var (stage, draw, protectedSlot, entryIds) = SeedCupWithAppliedSlotDraw();
        var placement = draw.Resolution.SlotResults.First(r => r.SlotKey == protectedSlot);
        var other = entryIds.First(id => !id.Equals(placement.EntryId));

        var act = () => AssignEntryToSlot.Execute(stage, protectedSlot, other);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.AppliedDrawSlotPlacementProtected);
    }

    [Fact]
    public void Assign_allows_same_entry_on_protected_slot()
    {
        var (stage, draw, protectedSlot, _) = SeedCupWithAppliedSlotDraw();
        var placement = draw.Resolution.SlotResults.First(r => r.SlotKey == protectedSlot);

        AssignEntryToSlot.Execute(stage, protectedSlot, placement.EntryId);

        stage.FindSlot(protectedSlot)!.EntryId.Should().Be(placement.EntryId);
    }

    [Fact]
    public void Assign_allows_on_resolution_slot_before_draw_apply()
    {
        var (stage, draw, protectedSlot, entryIds) = SeedCupWithAppliedSlotDraw(apply: false);
        DrawAppliedState.IsApplied(draw, stage).Should().BeFalse();

        AssignEntryToSlot.Execute(stage, protectedSlot, entryIds[0]);

        stage.FindSlot(protectedSlot)!.EntryId.Should().Be(entryIds[0]);
    }

    [Fact]
    public void Clear_allows_on_resolution_slot_before_draw_apply()
    {
        var (stage, draw, protectedSlot, _) = SeedCupWithAppliedSlotDraw(apply: false);
        DrawAppliedState.IsApplied(draw, stage).Should().BeFalse();
        draw.Resolution.SlotResults.Should().Contain(r => r.SlotKey == protectedSlot);

        ClearSlotAssignment.Execute(stage, protectedSlot);

        stage.FindSlot(protectedSlot)!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Assign_allows_on_slot_not_in_slot_results_when_draw_applied()
    {
        var (stage, draw, _, entryIds) = SeedCupWithAppliedSlotDraw(resolutionSlotCount: 2);
        DrawAppliedState.IsApplied(draw, stage).Should().BeTrue();
        var outsideSlot = stage.Slots
            .Select(s => s.SlotKey)
            .First(key => draw.Resolution.SlotResults.All(r => r.SlotKey != key));

        var appliedEntries = draw.Resolution.SlotResults.Select(r => r.EntryId).ToHashSet();
        var freeEntry = entryIds.First(id => !appliedEntries.Contains(id));
        AssignEntryToSlot.Execute(stage, outsideSlot, freeEntry);

        stage.FindSlot(outsideSlot)!.EntryId.Should().Be(freeEntry);
    }

    [Fact]
    public void Clear_allows_when_is_applied_false_after_occupancy_drift()
    {
        var (stage, draw, protectedSlot, entryIds) = SeedCupWithAppliedSlotDraw();
        var otherPlacement = draw.Resolution.SlotResults.First(r => r.SlotKey != protectedSlot);
        var driftEntry = entryIds.First(id => !id.Equals(otherPlacement.EntryId));
        stage.ApplyResolvedEntry(otherPlacement.SlotKey, driftEntry, _clock);
        DrawAppliedState.IsApplied(draw, stage).Should().BeFalse();

        var act = () => ClearSlotAssignment.Execute(stage, protectedSlot);

        act.Should().NotThrow<ApplicationFailureException>();
    }

    [Fact]
    public void Assign_uses_canonical_slot_key_when_input_has_surrounding_whitespace()
    {
        var (stage, draw, protectedSlot, _) = SeedCupWithAppliedSlotDraw();
        var placement = draw.Resolution.SlotResults.First(r => r.SlotKey == protectedSlot);
        var paddedKey = $"  {protectedSlot}  ";

        var act = () => AssignEntryToSlot.Execute(stage, paddedKey, placement.EntryId);

        act.Should().NotThrow();
        stage.FindSlot(protectedSlot)!.EntryId.Should().Be(placement.EntryId);
    }

    [Fact]
    public void Assign_still_hits_f1_domain_gate_when_b1_does_not_apply()
    {
        var competition = CreateCompetition.Execute("Cup-B1-F1", _clock);
        var stage = AddCompetitionStage.Execute(
            competition,
            StructureIntent.Cup(4, "KO"),
            _clock);
        var entryA = competition.AddEntry(TeamId.New(), "Team A", _clock);
        var entryB = competition.AddEntry(TeamId.New(), "Team B", _clock);
        var entryC = competition.AddEntry(TeamId.New(), "Team C", _clock);
        ReplaceStageAffectationAuthoring.Execute(
            stage,
            competition,
            [entryA.Id, entryB.Id, entryC.Id],
            _clock);

        stage.SeedEntryRoundBracketPairs();
        var round = stage.AddRound("Tour", _clock);
        stage.ApplyResolvedEntry("S1", entryA.Id, _clock);
        stage.ApplyResolvedEntry("S2", entryB.Id, _clock);
        var fixture = stage.AddFixture(round.Id, _clock, "S1", "S2", "P1");
        var match = Match.Create(competition.Id, stage.Id, entryA.Id, entryB.Id, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);

        var act = () => AssignEntryToSlot.Execute(stage, "S1", entryC.Id, [match]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
    }

    private (Stage Stage, Draw Draw, string ProtectedSlot, EntryId[] EntryIds) SeedCupWithAppliedSlotDraw(
        bool apply = true,
        int? resolutionSlotCount = null)
    {
        var competition = CreateCompetition.Execute("G-OPS3-B1", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock);
        var stage = configured.Stage;
        var entryIds = competition.Entries.Select(e => e.Id).ToArray();
        stage.ReplaceAffectationAuthoring(entryIds, _clock);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        var slotTargets = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets)
            .IsResolved.Should().BeTrue();

        if (resolutionSlotCount is { } keepCount && keepCount < slotTargets.Length)
        {
            var all = draw.Resolution.SlotResults.ToList();
            var trimmed = all.Skip(all.Count - keepCount).ToList();
            stage.RecordDrawResolution(
                draw.Id,
                DrawResolution.ResolvedSlots(trimmed),
                _clock);
            draw = stage.GetDraw(draw.Id);
        }

        PublishDraw.Execute(stage, draw.Id, _clock);

        if (apply)
        {
            ApplyDraw.Execute(stage, draw.Id, _clock);
            DrawAppliedState.IsApplied(draw, stage).Should().BeTrue();
        }

        var protectedSlot = draw.Resolution.SlotResults[0].SlotKey;
        return (stage, draw, protectedSlot, entryIds);
    }
}
