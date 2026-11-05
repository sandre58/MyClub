// -----------------------------------------------------------------------
// <copyright file="StageDrawDisplayTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Origin derivation, withdrawn counting, and historical non-fusion for the Tirage read model.
/// </summary>
public sealed class StageDrawDisplayTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Draft_resolution_line_is_drawn_with_origin_draw()
    {
        var (competition, stage, entries) = Cup(4);
        var preserved = stage.Slots[0].SlotKey;
        stage.AssignEntryToSlot(preserved, entries[0]);
        var draw = ResolvePartial(stage);

        var overview = StageOverviewAssembler.Assemble(stage, competition);
        var active = overview.Draws.Single(item => item.Id == draw.Id.Value);

        active.InputsIntent.Should().Be(DrawInputsIntent.Partial);
        active.SlotPlacements.Should().NotBeEmpty();
        active.SlotPlacements.Should().OnlyContain(placement => placement.Origin == DrawPlacementOrigin.Draw);
        active.SlotPlacements.Should().NotContain(placement => placement.SlotKey == preserved);
        active.SlotDisplayCells.Should().NotBeNull();
        active.SlotDisplayCells!.Single(cell => cell.SlotKey == preserved).CellState
            .Should().Be(SlotDrawCellState.AlreadyPlaced);
        active.SlotDisplayCells.Where(cell => cell.SlotKey != preserved)
            .Should().OnlyContain(cell =>
                cell.CellState == SlotDrawCellState.Drawn && cell.Origin == DrawPlacementOrigin.Draw);
        active.DisplayMetrics.Should().NotBeNull();
        active.DisplayMetrics!.VacantSlotKeyCount.Should().Be(active.DisplayMetrics.VacantSlotKeys.Count);
        active.DisplayMetrics.Should().BeEquivalentTo(overview.SlotDrawDisplayMetrics);
    }

    [Fact]
    public void Qualification_feed_without_a_resolution_entry_is_reserved_not_withdrawn()
    {
        var competition = CreateCompetition.Execute("Qual origin", _clock);
        var home = AddEntry.Execute(competition, "A", _clock).Id;
        var away = AddEntry.Execute(competition, "B", _clock).Id;
        var cup = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock).Stage;
        ReplaceStageDrawRules.Execute(cup, new DrawRules(DrawMode.Random), _clock);
        cup.ReplaceAffectationAuthoring([home, away], _clock);
        var fed = cup.Slots[0].SlotKey;
        cup.ApplyResolvedEntry(fed, home, _clock);
        var league = Stage.Create(competition.Id, new StageName("League"), SampleRegulations.Standard(), _clock);
        league.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(cup.Id, fed)),
            ]),
            _clock);
        competition.AddStage(league.Id, _clock);
        var draw = CreateDraw.Execute(cup, DrawResolutionKind.Slot, _clock);

        var overview = StageOverviewAssembler.Assemble(cup, competition, [league, cup]);
        var cell = overview.Draws.Single(item => item.Id == draw.Id.Value)
            .SlotDisplayCells!.Single(item => item.SlotKey == fed);

        cell.CellState.Should().Be(SlotDrawCellState.HeldByFeed);
        cell.Origin.Should().Be(DrawPlacementOrigin.Feed);
        cell.CellState.Should().NotBe(SlotDrawCellState.Withdrawn);
        cell.EntryId.Should().Be(home.Value);
    }

    [Fact]
    public void Direct_assignment_is_manual_already_placed()
    {
        var (competition, stage, entries) = Cup(2);
        var key = stage.Slots[0].SlotKey;
        stage.AssignEntryToSlot(key, entries[0]);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        var cell = StageOverviewAssembler.Assemble(stage, competition)
            .Draws.Single(item => item.Id == draw.Id.Value)
            .SlotDisplayCells!.Single(item => item.SlotKey == key);

        cell.CellState.Should().Be(SlotDrawCellState.AlreadyPlaced);
        cell.Origin.Should().Be(DrawPlacementOrigin.Manual);
        cell.Origin.Should().NotBe(DrawPlacementOrigin.Draw);
        cell.Origin.Should().NotBe(DrawPlacementOrigin.Feed);
    }

    [Fact]
    public void Unique_direct_feed_without_an_occupant_is_reserved_not_already_placed()
    {
        var (competition, stage, entries) = Cup(2);
        var key = stage.Slots[0].SlotKey;
        stage.AssignEntryToSlot(key, entries[0]);

        // AssignEntryToSlot writes the assignment and the occupant together.
        // ClearResolvedEntry refuses a direct assignment, and ClearSlotAssignment removes both.
        // The feed snapshot still represents a unique direct feed with an empty slot.
        var slot = stage.FindSlot(key)!;
        typeof(Slot).GetMethod("ClearEntry", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(slot, null);

        slot.EntryId.Should().BeNull();
        stage.DirectAssignments.Should().ContainSingle(assignment => assignment.SlotKey == key);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        var cell = StageOverviewAssembler.Assemble(stage, competition)
            .Draws.Single(item => item.Id == draw.Id.Value)
            .SlotDisplayCells!.Single(item => item.SlotKey == key);

        cell.CellState.Should().Be(SlotDrawCellState.HeldByFeed);
        cell.Origin.Should().Be(DrawPlacementOrigin.Feed);
        cell.CellState.Should().NotBe(SlotDrawCellState.Free);
        cell.CellState.Should().NotBe(SlotDrawCellState.AlreadyPlaced);
        cell.EntryId.Should().BeNull();
    }

    [Fact]
    public void HeldByFeed_serializes_as_Reserved()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        var json = JsonSerializer.Serialize(SlotDrawCellState.HeldByFeed, options);

        json.Should().Be("\"Reserved\"");
        JsonSerializer.Deserialize<SlotDrawCellState>(json, options)
            .Should().Be(SlotDrawCellState.HeldByFeed);
    }

    [Fact]
    public void Resolved_occupant_without_a_feed_is_manual()
    {
        var (competition, stage, entries) = Cup(2);
        var key = stage.Slots[0].SlotKey;
        stage.ApplyResolvedEntry(key, entries[0], _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        var cell = StageOverviewAssembler.Assemble(stage, competition)
            .Draws.Single(item => item.Id == draw.Id.Value)
            .SlotDisplayCells!.Single(item => item.SlotKey == key);

        cell.CellState.Should().Be(SlotDrawCellState.AlreadyPlaced);
        cell.Origin.Should().Be(DrawPlacementOrigin.Manual);
    }

    [Fact]
    public void Occupant_from_a_cancelled_published_draw_is_manual_on_the_new_draft()
    {
        var (competition, stage, _) = Cup(4);
        var keys = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        var first = ResolveFull(stage);
        PublishDraw.Execute(stage, first.Id, _clock);
        ApplyDraw.Execute(stage, first.Id, _clock);
        CancelDraw.Execute(stage, first.Id, _clock);
        var preservedId = stage.FindSlot(keys[0])!.EntryId;
        stage.ClearResolvedEntry(keys[1], _clock);
        stage.ClearResolvedEntry(keys[2], _clock);
        stage.ClearResolvedEntry(keys[3], _clock);
        var draft = ResolvePartial(stage);

        var overview = StageOverviewAssembler.Assemble(stage, competition);
        var preserved = overview.Draws.Single(item => item.Id == draft.Id.Value)
            .SlotDisplayCells!.Single(cell => cell.SlotKey == keys[0]);

        preserved.CellState.Should().Be(SlotDrawCellState.AlreadyPlaced);
        preserved.Origin.Should().Be(DrawPlacementOrigin.Manual);
        preserved.EntryId.Should().Be(preservedId!.Value.Value);
        overview.Draws.Single(item => item.Id == draft.Id.Value)
            .SlotPlacements.Should().NotContain(placement => placement.SlotKey == keys[0]);
    }

    [Fact]
    public void Withdrawn_occupant_is_not_vacant_and_does_not_change_unplaced_composition()
    {
        var (competition, stage, entries) = Cup(4);
        var withdrawnKey = stage.Slots[0].SlotKey;
        stage.ApplyResolvedEntry(withdrawnKey, entries[0], _clock);
        stage.RemoveCompositionEntryIfPresent(entries[0], _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);

        var overview = StageOverviewAssembler.Assemble(stage, competition);
        var metrics = overview.SlotDrawDisplayMetrics!;
        var cell = overview.Draws.Single(item => item.Id == draw.Id.Value)
            .SlotDisplayCells!.Single(item => item.SlotKey == withdrawnKey);

        cell.CellState.Should().Be(SlotDrawCellState.Withdrawn);
        cell.Origin.Should().Be(DrawPlacementOrigin.Manual);
        cell.CellState.Should().NotBe(SlotDrawCellState.HeldByFeed);
        metrics.VacantSlotKeys.Should().NotContain(withdrawnKey);
        metrics.UnplacedEntryCount.Should().Be(3);
        metrics.AlreadyPlacedEntryCount.Should().Be(0);
        metrics.EntryCount.Should().Be(3);
        overview.Draws.Single(item => item.Id == draw.Id.Value).DisplayMetrics.Should().BeEquivalentTo(metrics);
    }

    [Fact]
    public void Published_partial_cells_ignore_a_later_manual_release()
    {
        var (competition, stage, entries) = Cup(4);
        var keys = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        stage.ApplyResolvedEntry(keys[0], entries[0], _clock);
        var draw = ResolvePartial(stage);
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        var before = Cells(stage, competition, draw.Id.Value);
        var released = before.First(cell => cell.CellState == SlotDrawCellState.Drawn);
        stage.ClearResolvedEntry(released.SlotKey, _clock);
        var after = Cells(stage, competition, draw.Id.Value);

        after.Should().BeEquivalentTo(before);
        after.Should().OnlyContain(cell =>
            cell.CellState == SlotDrawCellState.Drawn
            || cell.CellState == SlotDrawCellState.OutsideDraw);
        after.Should().Contain(cell =>
            cell.SlotKey == keys[0]
            && cell.CellState == SlotDrawCellState.OutsideDraw
            && cell.EntryId == null);
        after.Should().Contain(cell =>
            cell.SlotKey == released.SlotKey
            && cell.CellState == SlotDrawCellState.Drawn
            && cell.EntryId == released.EntryId
            && cell.Origin == DrawPlacementOrigin.Draw);
        StageOverviewAssembler.Assemble(stage, competition)
            .Draws.Single(item => item.Id == draw.Id.Value)
            .DisplayMetrics.Should().BeNull();
    }

    [Fact]
    public void Group_rerun_does_not_project_already_placed_cells()
    {
        var competition = CreateCompetition.Execute("Groups rerun", _clock);
        var entries = Enumerable.Range(0, 4)
            .Select(index => AddEntry.Execute(competition, $"G{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Groups(2, 2), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(entries, _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputs.ForGroup(entries),
            DrawInputsIntent.Rerun,
            [stage]);

        var projected = StageOverviewAssembler.Assemble(stage, competition).Draws.Single();

        projected.InputsIntent.Should().Be(DrawInputsIntent.Rerun);
        projected.Kind.Should().Be(DrawResolutionKind.Group);
        projected.SlotDisplayCells.Should().BeNull();
        projected.DisplayMetrics.Should().BeNull();
        projected.SlotPlacements.Should().BeEmpty();
    }

    private static IReadOnlyList<StageDrawSlotDisplayCellDto> Cells(
        Stage stage,
        Competition competition,
        Guid drawId) =>
        StageOverviewAssembler.Assemble(stage, competition)
            .Draws.Single(item => item.Id == drawId)
            .SlotDisplayCells!;

    private Draw ResolvePartial(Stage stage)
    {
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreatePartial(stage, [stage], DrawResolutionKind.Slot);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs, DrawInputsIntent.Partial, [stage]);
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            stage.Slots.Select(slot => slot.SlotKey).ToArray(),
            competitionStages: [stage],
            randomSource: new SeededRandomSource(3));
        return draw;
    }

    private Draw ResolveFull(Stage stage)
    {
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs, DrawInputsIntent.Default, [stage]);
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            stage.Slots.Select(slot => slot.SlotKey).ToArray(),
            competitionStages: [stage],
            randomSource: new SeededRandomSource(1));
        return draw;
    }

    private (Competition Competition, Stage Stage, EntryId[] Entries) Cup(int slots)
    {
        var competition = CreateCompetition.Execute($"Cup {slots}", _clock);
        var entries = Enumerable.Range(0, slots)
            .Select(index => AddEntry.Execute(competition, $"T{index}", _clock).Id)
            .ToArray();
        var stage = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(slots), _clock).Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring(entries, _clock);
        return (competition, stage, entries);
    }
}
