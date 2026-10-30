// -----------------------------------------------------------------------
// <copyright file="PartialSlotDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class PartialSlotDrawTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 11, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Partial_draw_fills_only_free_slots_and_keeps_manual_and_previous_occupants()
    {
        var (stage, entries) = Cup(4);
        var manualKey = stage.Slots[0].SlotKey;
        var previousKey = stage.Slots[1].SlotKey;
        stage.AssignEntryToSlot(manualKey, entries[0]);
        stage.ApplyResolvedEntry(previousKey, entries[1], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreatePartial(stage, [stage], DrawResolutionKind.Slot);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs, DrawInputsIntent.Partial, [stage]);
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            stage.Slots.Select(slot => slot.SlotKey).ToArray(),
            competitionStages: [stage],
            randomSource: new SeededRandomSource(1));

        stage.FindSlot(manualKey)!.EntryId.Should().Be(entries[0]);
        stage.FindSlot(previousKey)!.EntryId.Should().Be(entries[1]);
        stage.Slots.Skip(2).Should().OnlyContain(slot => slot.EntryId == null);
        var resolution = stage.GetDraw(draw.Id).Resolution.SlotResults;
        resolution.Select(placement => placement.SlotKey).Should().BeEquivalentTo(
            stage.Slots.Skip(2).Select(slot => slot.SlotKey));
        resolution.Select(placement => placement.EntryId).Should().BeEquivalentTo([entries[2], entries[3]]);

        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        stage.FindSlot(manualKey)!.EntryId.Should().Be(entries[0]);
        stage.DirectAssignments.Should().ContainSingle(assignment => assignment.SlotKey == manualKey);
        stage.FindSlot(previousKey)!.EntryId.Should().Be(entries[1]);
        stage.Slots.Select(slot => slot.EntryId).Should().BeEquivalentTo(entries);

        CancelDraw.Execute(stage, draw.Id, _clock);
        var released = ReleaseDrawAlignedPlacements.Execute(stage, draw.Id, _clock);
        released.ReleasedCount.Should().Be(2);
        stage.FindSlot(manualKey)!.EntryId.Should().Be(entries[0]);
        stage.FindSlot(previousKey)!.EntryId.Should().Be(entries[1]);
        stage.Slots.Skip(2).Should().OnlyContain(slot => slot.EntryId == null);
    }

    [Fact]
    public void Generate_does_not_move_occupants_and_redo_of_an_unapplied_draw_is_refused()
    {
        var (stage, entries) = Cup(2);
        var occupiedKey = stage.Slots[0].SlotKey;
        stage.AssignEntryToSlot(occupiedKey, entries[0]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreatePartial(stage, [stage], DrawResolutionKind.Slot);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs, DrawInputsIntent.Partial, [stage]);
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            stage.Slots.Select(slot => slot.SlotKey).ToArray(),
            competitionStages: [stage],
            randomSource: new SeededRandomSource(2));

        stage.FindSlot(occupiedKey)!.EntryId.Should().Be(entries[0]);
        stage.FindSlot(stage.Slots[1].SlotKey)!.EntryId.Should().BeNull();
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Draft);

        var act = () => RedoDraw.Execute(stage, draw.Id, _clock, [stage]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawRedoRequiresApplied);
        stage.FindSlot(occupiedKey)!.EntryId.Should().Be(entries[0]);
        stage.FindSlot(stage.Slots[1].SlotKey)!.EntryId.Should().BeNull();
        stage.GetDraw(draw.Id).Status.Should().Be(DrawStatus.Draft);
    }

    [Fact]
    public void Partial_refuses_no_free_slot_not_enough_teams_and_too_many_teams()
    {
        var full = Cup(2);
        full.Stage.ApplyResolvedEntry(full.Stage.Slots[0].SlotKey, full.Entries[0], _clock);
        full.Stage.ApplyResolvedEntry(full.Stage.Slots[1].SlotKey, full.Entries[1], _clock);
        ActPartial(full.Stage).Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawNoFreeSlot);
        full.Stage.Slots.Should().OnlyContain(slot => slot.EntryId != null);

        var shortPool = Cup(4, teams: 2);
        shortPool.Stage.ApplyResolvedEntry(
            shortPool.Stage.Slots[0].SlotKey,
            shortPool.Entries[0],
            _clock);
        ActPartial(shortPool.Stage).Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawNotEnoughTeams);

        var extra = Cup(2, teams: 3);
        extra.Stage.ApplyResolvedEntry(extra.Stage.Slots[0].SlotKey, extra.Entries[0], _clock);
        ActPartial(extra.Stage).Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawTooManyTeams);
        extra.Stage.FindSlot(extra.Stage.Slots[1].SlotKey)!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Apply_refuses_to_move_a_team_that_already_occupies_another_slot()
    {
        var (stage, entries) = Cup(2);
        var occupiedKey = stage.Slots[0].SlotKey;
        var freeKey = stage.Slots[1].SlotKey;
        stage.ApplyResolvedEntry(occupiedKey, entries[0], _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entries[0]]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entries[0], freeKey)]),
            _clock);
        PublishDraw.Execute(stage, draw.Id, _clock);

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot(occupiedKey)!.EntryId.Should().Be(entries[0]);
        stage.FindSlot(freeKey)!.EntryId.Should().BeNull();
    }

    [Fact]
    public void Reserved_qualification_slot_is_not_a_free_target()
    {
        var competition = CreateCompetition.Execute("Qual partial", _clock);
        var home = AddEntry.Execute(competition, "A", _clock).Id;
        var away = AddEntry.Execute(competition, "B", _clock).Id;
        var cup = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock).Stage;
        ReplaceStageDrawRules.Execute(cup, new DrawRules(DrawMode.Random), _clock);
        cup.ReplaceAffectationAuthoring([home, away], _clock);
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(cup.Id, cup.Slots[0].SlotKey)),
            ]),
            _clock);
        competition.AddStage(league.Id, _clock);

        var act = () => DrawInputsFactory.CreatePartial(cup, [league, cup], DrawResolutionKind.Slot);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawTooManyTeams);
    }

    private Action ActPartial(Stage stage)
    {
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        return () =>
        {
            var inputs = DrawInputsFactory.CreatePartial(stage, [stage], DrawResolutionKind.Slot);
            ConfigureDrawInputs.Execute(stage, draw.Id, inputs, DrawInputsIntent.Partial, [stage]);
        };
    }

    private (Stage Stage, EntryId[] Entries) Cup(int slots, int? teams = null) =>
        CupCore(slots, teams ?? slots);

    private (Stage Stage, EntryId[] Entries) CupCore(int slots, int teams)
    {
        var competition = CreateCompetition.Execute($"Cup {slots}", _clock);
        var entries = new EntryId[teams];
        for (var index = 0; index < teams; index++)
        {
            entries[index] = AddEntry.Execute(competition, $"T{index}", _clock).Id;
        }

        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(slots), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(entries, _clock);
        return (stage, entries);
    }
}
