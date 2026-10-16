// -----------------------------------------------------------------------
// <copyright file="StageDrawRerunLaunchGuardTests.cs" company="Stéphane ANDRE">
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

public sealed class StageDrawRerunLaunchGuardTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Rerun_configure_inputs_rejects_occupied_slot()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var entries = Enumerable.Range(0, 4)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(4), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([.. entries], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        ApplyDrawPipelineToFillSlots(stage, draw);
        CancelDraw.Execute(stage, draw.Id, _clock);

        var draw2 = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var rerunInputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot, DrawInputsIntent.Rerun);
        var act = () => ConfigureDrawInputs.Execute(
            stage,
            draw2.Id,
            rerunInputs,
            DrawInputsIntent.Rerun,
            [stage]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawLaunchGridNotVacant);
    }

    [Fact]
    public void Rerun_configure_inputs_rejects_qual_reserved_slot()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("KO"), SampleRegulations.Standard(), _clock);
        target.AddRound("R1", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");
        target.AddSlot("SF1-C");
        target.AddSlot("SF1-D");
        ReplaceStageDrawRules.Execute(target, new DrawRules(DrawMode.Random), _clock);
        var entries = Enumerable.Range(0, 4).Select(_ => EntryId.New()).ToArray();
        target.ReplaceAffectationAuthoring(entries, _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(target.Id, "SF1-A"))
            ]),
            _clock);

        var draw = CreateDraw.Execute(target, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreateDefault(target, DrawResolutionKind.Slot, DrawInputsIntent.Rerun);

        var act = () => ConfigureDrawInputs.Execute(
            target,
            draw.Id,
            inputs,
            DrawInputsIntent.Rerun,
            [source, target]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawLaunchGridNotVacant);
    }

    [Fact]
    public void Rerun_configure_inputs_rejects_direct_assignment_before_occupied()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([e1.Id, e2.Id], _clock);
        stage.AssignEntryToSlot(stage.Slots[0].SlotKey, e1.Id);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot, DrawInputsIntent.Rerun);

        var act = () => ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            inputs,
            DrawInputsIntent.Rerun,
            [stage]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawLaunchDirectAssignmentPresent);
    }

    [Fact]
    public void Default_configure_inputs_allows_partially_occupied_grid()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([e1.Id, e2.Id], _clock);
        stage.AssignEntryToSlot(stage.Slots[0].SlotKey, e1.Id);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot, DrawInputsIntent.Default);

        ConfigureDrawInputs.Execute(stage, draw.Id, inputs, DrawInputsIntent.Default, [stage]);

        stage.GetDraw(draw.Id).Inputs!.FixedSlots.Should().ContainSingle();
    }

    [Fact]
    public void Rerun_configure_inputs_succeeds_on_vacant_grid()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var entries = Enumerable.Range(0, 2)
            .Select(i => AddEntry.Execute(competition, $"T{i}", _clock).Id)
            .ToArray();
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([.. entries], _clock);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Slot, _clock);
        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot, DrawInputsIntent.Rerun);

        ConfigureDrawInputs.Execute(
            stage,
            draw.Id,
            inputs,
            DrawInputsIntent.Rerun,
            [stage]);

        stage.GetDraw(draw.Id).Inputs!.FixedSlots.Should().BeEmpty();
    }

    private void ApplyDrawPipelineToFillSlots(Stage stage, Draw draw)
    {
        var defaultInputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Slot);
        ConfigureDrawInputs.Execute(stage, draw.Id, defaultInputs);
        var slotTargets = stage.Slots.Select(slot => slot.SlotKey).ToArray();
        GenerateDrawResolution.Execute(stage, draw.Id, _clock, slotTargets);
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.Slots.Should().OnlyContain(slot => slot.EntryId != null);
    }
}
