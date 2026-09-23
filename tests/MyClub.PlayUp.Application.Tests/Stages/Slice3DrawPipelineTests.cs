// -----------------------------------------------------------------------
// <copyright file="Slice3DrawPipelineTests.cs" company="Stéphane ANDRE">
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

public sealed class Slice3DrawPipelineTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 16, 13, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Groups_create_configure_generate_publish_apply_then_materialize()
    {
        var competition = CreateCompetition.Execute("G", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);

        stage.ReplaceCompositionEntries([.. competition.Entries.Select(e => e.Id)], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs);

        var generated = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(group => group.Id)]);
        generated.IsResolved.Should().BeTrue();

        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.Groups.Sum(group => group.EntryIds.Count).Should().Be(4);

        var materialized = MaterializeMatches.Execute(competition, stage, [], _clock);
        materialized.CreatedMatches.Should().HaveCount(2);
    }

    [Fact]
    public void Cup_slot_pipeline_occupies_places_via_apply()
    {
        var competition = CreateCompetition.Execute("CupSlot", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"S{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock);
        var stage = configured.Stage;
        stage.Slots.Should().HaveCount(4);

        stage.ReplaceCompositionEntries([.. competition.Entries.Select(e => e.Id)], _clock);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        var slotTargets = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets)
            .IsResolved.Should().BeTrue();
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        stage.Slots.Should().OnlyContain(slot => slot.EntryId != null);
        draw.Resolution.SlotResults.Select(r => r.SlotKey)
            .Should().BeEquivalentTo(slotTargets);
    }

    [Fact]
    public void Generate_after_publish_is_rejected()
    {
        var competition = CreateCompetition.Execute("Pub", _clock);
        AddEntry.Execute(competition, "A", _clock);
        AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(2),
            _clock);
        var stage = configured.Stage;
        stage.ReplaceCompositionEntries([.. competition.Entries.Select(e => e.Id)], _clock);
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot));
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: [.. stage.Slots.Select(slot => slot.SlotKey)]);
        PublishDraw.Execute(stage, draw.Id, _clock);

        var act = () => GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: [.. stage.Slots.Select(slot => slot.SlotKey)]);
        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawGenerationFailure);
    }
}
