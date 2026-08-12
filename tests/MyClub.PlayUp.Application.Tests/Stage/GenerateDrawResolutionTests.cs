// -----------------------------------------------------------------------
// <copyright file="GenerateDrawResolutionTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stage;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Application.Tests.Stage;

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
            stage.AddSlot(key, _clock);
        }

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(entries), _clock);

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
    public void Execute_no_solution_marks_draw_no_solution()
    {
        var stage = CreateStageWithSameGroupRequired();
        var entries = NewEntries(4);
        var g = GroupId.New();
        var groups = entries.ToDictionary(e => e, _ => g);
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing(entries), _clock);

        var result = GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            constraintContext: new DrawConstraintContext(groups, null),
            seed: 1);

        result.IsNoSolution.Should().BeTrue();
        draw.Resolution.State.Should().Be(DrawResolutionState.NoSolution);
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
        stage.AddSlot("A", _clock);
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]), _clock);
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
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(NewEntries(2)), _clock);

        var act = () => GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            slotTargets: ["OnlyOne"],
            seed: 1);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawGenerationInvalid);
        draw.Resolution.State.Should().Be(DrawResolutionState.NotResolved);
    }

    private static EntryId[] NewEntries(int count) =>
        [..Enumerable.Range(0, count).Select(_ => EntryId.New())];

    private StageAggregate CreateStage() =>
        StageAggregate.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);

    private StageAggregate CreateStageWithSameGroupRequired()
    {
        var stage = CreateStage();
        stage.ReplaceDrawRules(
            new DrawRules(
                DrawMode.Random,
                constraints:
                [
                    new DrawConstraint(DrawConstraintType.SameGroupAvoidance, ConstraintEnforcement.Required)
                ]),
            _clock);
        return stage;
    }

    private Draw PrepareSlotDraw(StageAggregate stage, IReadOnlyList<EntryId> entries, IReadOnlyList<string> targets)
    {
        foreach (var key in targets)
        {
            stage.AddSlot(key, _clock);
        }

        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot(entries), _clock);
        return draw;
    }
}
