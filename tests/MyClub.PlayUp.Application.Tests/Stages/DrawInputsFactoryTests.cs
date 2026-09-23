// -----------------------------------------------------------------------
// <copyright file="DrawInputsFactoryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class DrawInputsFactoryTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 22, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public void CreateDefault_fails_when_composition_empty_even_if_active_entries_exist()
    {
        var competition = CreateCompetition.Execute("FailClosed", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(2),
            _clock).Stage;

        var act = () => DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Pairing);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawGenerationFailure);
    }

    [Fact]
    public void CreateDefault_group_encoding_F_includes_composition_and_fixed_group_placements()
    {
        var competition = CreateCompetition.Execute("EncF", _clock);
        var ids = new List<Domain.Common.EntryId>();
        for (var i = 0; i < 4; i++)
        {
            ids.Add(AddEntry.Execute(competition, $"T{i}", _clock).Id);
        }

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceCompositionEntries(ids, _clock);

        var fixedGroup = stage.Groups[0];
        stage.AssignEntryToGroup(fixedGroup.Id, ids[0]);

        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group);

        inputs.Entries.Should().BeEquivalentTo(ids);
        inputs.FixedGroups.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new GroupDrawPlacement(ids[0], fixedGroup.Id));
    }

    [Fact]
    public void CreateDefault_group_rerun_ignores_occupancy_fixed_groups()
    {
        var competition = CreateCompetition.Execute("Rerun", _clock);
        var ids = new List<Domain.Common.EntryId>();
        for (var i = 0; i < 4; i++)
        {
            ids.Add(AddEntry.Execute(competition, $"T{i}", _clock).Id);
        }

        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock).Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceCompositionEntries(ids, _clock);

        var fixedGroup = stage.Groups[0];
        stage.AssignEntryToGroup(fixedGroup.Id, ids[0]);

        var inputs = DrawInputsFactory.CreateDefault(
            stage,
            DrawResolutionKind.Group,
            DrawInputsIntent.Rerun);

        inputs.Entries.Should().BeEquivalentTo(ids);
        inputs.FixedGroups.Should().BeEmpty();
    }

    [Fact]
    public void CreateDefault_slot_encoding_F_includes_fixed_slot_placements()
    {
        var competition = CreateCompetition.Execute("SlotF", _clock);
        var a = AddEntry.Execute(competition, "A", _clock).Id;
        var b = AddEntry.Execute(competition, "B", _clock).Id;
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(2),
            _clock).Stage;
        stage.ReplaceCompositionEntries([a, b], _clock);

        var slotKey = stage.Slots[0].SlotKey;
        stage.ApplyResolvedEntry(slotKey, a, _clock);

        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot);

        inputs.Entries.Should().BeEquivalentTo([a, b]);
        inputs.FixedSlots.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new SlotDrawPlacement(a, slotKey));
    }
}
