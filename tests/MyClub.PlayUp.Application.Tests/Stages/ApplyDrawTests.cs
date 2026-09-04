// -----------------------------------------------------------------------
// <copyright file="ApplyDrawTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ApplyDrawTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 11, 14, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void Execute_rejects_missing_draw()
    {
        var stage = CreateStage();

        var act = () => ApplyDraw.Execute(stage, DrawId.New(), _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.DrawNotFound);
    }

    [Fact]
    public void Execute_rejects_draft_draw()
    {
        var stage = CreateStage();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        var entry = EntryId.New();
        stage.AddSlot("A");
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "A")]),
            _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Draft);
    }

    [Fact]
    public void Execute_rejects_cancelled_draw()
    {
        var stage = CreateStage();
        var (draw, _) = PublishSlotDraw(stage, "A");
        stage.CancelDraw(draw.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Cancelled);
    }

    [Fact]
    public void Execute_rejects_not_resolved_draw()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]));
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Resolution.State.Should().Be(DrawResolutionState.NotResolved);
    }

    [Fact]
    public void Execute_rejects_no_solution_draw()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([EntryId.New()]));
        stage.MarkDrawNoSolution(draw.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Resolution.State.Should().Be(DrawResolutionState.NoSolution);
        draw.Status.Should().Be(DrawStatus.Draft);
    }

    [Fact]
    public void Execute_pairing_without_context_is_rejected()
    {
        var stage = CreateStage();
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_slot_applies_resolution()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage, "A");

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().ContainSingle();
        result.SlotInstructions[0].EntryId.Should().Be(entry);
        result.CreatedMatches.Should().BeEmpty();
        stage.FindSlot("A")!.EntryId.Should().Be(entry);
        draw.Status.Should().Be(DrawStatus.Published);
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    [Fact]
    public void Execute_slot_idempotent_when_already_conform()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage, "A");
        ApplyDraw.Execute(stage, draw.Id, _clock);
        stage.ClearDomainEvents();

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().ContainSingle();
        stage.FindSlot("A")!.EntryId.Should().Be(entry);
        stage.DomainEvents.Should().NotContain(e => e is StageSlotOccupantChanged);
    }

    [Fact]
    public void Execute_slot_applies_when_stage_running()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        var (draw, entry) = PublishSlotDraw(stage, "A");
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.ClearDomainEvents();

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().ContainSingle();
        stage.Status.Should().Be(StageStatus.Running);
        stage.FindSlot("A")!.EntryId.Should().Be(entry);
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_slot_applies_when_stage_suspended()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        var (draw, entry) = PublishSlotDraw(stage, "A");
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Suspend(_clock);
        stage.ClearDomainEvents();

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().ContainSingle();
        stage.Status.Should().Be(StageStatus.Suspended);
        stage.FindSlot("A")!.EntryId.Should().Be(entry);
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_slot_rejects_when_stage_completed()
    {
        var stage = CreateStage();
        stage.AddRound("R1", _clock);
        var (draw, _) = PublishSlotDraw(stage, "A");
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Complete(_clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
        stage.Status.Should().Be(StageStatus.Completed);
    }

    [Fact]
    public void Execute_slot_rejects_divergent_occupant()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage, "A");
        var other = EntryId.New();
        stage.ApplyResolvedEntry("A", other, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().Be(other);
        stage.DomainEvents.Should().BeEmpty();
        _ = entry;
    }

    [Fact]
    public void Execute_slot_rejects_missing_slot_without_mutation()
    {
        var stage = CreateStage();
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "Missing")]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        stage.AddSlot("A");
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_slot_rejects_direct_assignment_conflict()
    {
        var stage = CreateStage();
        var (draw, entry) = PublishSlotDraw(stage, "A");
        var direct = EntryId.New();
        stage.AssignEntryToSlot("A", direct);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().Be(direct);
        stage.DomainEvents.Should().BeEmpty();
        _ = entry;
    }

    [Fact]
    public void Execute_slot_preflight_failure_mutates_nothing_on_second_slot()
    {
        var stage = CreateStage();
        stage.AddSlot("A");
        stage.AddSlot("B");
        var e1 = EntryId.New();
        var e2 = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([e1, e2]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots(
            [
                new SlotDrawPlacement(e1, "A"),
                new SlotDrawPlacement(e2, "B")
            ]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        stage.ApplyResolvedEntry("B", EntryId.New(), _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.FindSlot("A")!.EntryId.Should().BeNull();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_group_applies_resolution()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, group.Id);

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().BeEmpty();
        result.CreatedMatches.Should().BeEmpty();
        group.EntryIds.Should().ContainSingle().Which.Should().Be(entry);
        draw.Status.Should().Be(DrawStatus.Published);
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    [Fact]
    public void Execute_group_idempotent_when_already_conform()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, group.Id);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        var result = ApplyDraw.Execute(stage, draw.Id, _clock);

        result.SlotInstructions.Should().BeEmpty();
        group.EntryIds.Should().ContainSingle().Which.Should().Be(entry);
    }

    [Fact]
    public void Execute_group_rejects_entry_in_other_group()
    {
        var stage = CreateStage();
        var groupA = stage.AddGroup("A", _clock);
        var groupB = stage.AddGroup("B", _clock);
        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, groupA.Id);
        stage.AssignEntryToGroup(groupB.Id, entry);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        groupA.EntryIds.Should().BeEmpty();
        groupB.EntryIds.Should().ContainSingle().Which.Should().Be(entry);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_group_rejects_missing_group_without_mutation()
    {
        var stage = CreateStage();
        var existing = stage.AddGroup("A", _clock);
        var entry = EntryId.New();
        var missingGroupId = GroupId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, missingGroupId)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        existing.EntryIds.Should().BeEmpty();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_group_rejects_when_structure_locked()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var prepareEntry = EntryId.New();
        stage.AssignEntryToGroup(group.Id, prepareEntry);
        stage.AddMatchday(1, _clock);

        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, group.Id);

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        group.EntryIds.Should().Equal(prepareEntry);
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_group_rejects_when_stage_suspended()
    {
        var stage = CreateStage();
        var group = stage.AddGroup("A", _clock);
        var prepareEntry = EntryId.New();
        stage.AssignEntryToGroup(group.Id, prepareEntry);
        stage.AddMatchday(1, _clock);

        var entry = EntryId.New();
        var draw = PublishGroupDraw(stage, entry, group.Id);

        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.Suspend(_clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(stage, draw.Id, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        group.EntryIds.Should().Equal(prepareEntry);
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
        stage.Status.Should().Be(StageStatus.Suspended);
    }

    [Fact]
    public void Execute_pairing_creates_and_attaches_matches()
    {
        var stage = CreateStage();
        var round = stage.AddRound("R1", _clock);
        var fixture1 = stage.AddFixture(round.Id, _clock);
        var fixture2 = stage.AddFixture(round.Id, _clock);
        var a = EntryId.New();
        var b = EntryId.New();
        var c = EntryId.New();
        var d = EntryId.New();
        var draw = PublishPairingDraw(
            stage,
            [new PairingDrawResult(a, b), new PairingDrawResult(c, d)],
            [a, b, c, d]);

        var result = ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext([fixture1.Id, fixture2.Id]),
            []);

        result.CreatedMatches.Should().HaveCount(2);
        result.CreatedMatches[0].HomeEntryId.Should().Be(a);
        result.CreatedMatches[0].AwayEntryId.Should().Be(b);
        result.CreatedMatches[0].CompetitionId.Should().Be(_competitionId);
        result.CreatedMatches[0].StageId.Should().Be(stage.Id);
        result.CreatedMatches[1].HomeEntryId.Should().Be(c);
        result.CreatedMatches[1].AwayEntryId.Should().Be(d);
        fixture1.MatchIds.Should().ContainSingle().Which.Should().Be(result.CreatedMatches[0].Id);
        fixture2.MatchIds.Should().ContainSingle().Which.Should().Be(result.CreatedMatches[1].Id);
        fixture1.Attachments.Should().ContainSingle().Which.LegIndex.Should().Be(1);
        fixture2.Attachments.Should().ContainSingle().Which.LegIndex.Should().Be(1);
        draw.Status.Should().Be(DrawStatus.Published);
        draw.Resolution.State.Should().Be(DrawResolutionState.Resolved);
    }

    [Fact]
    public void Execute_pairing_second_apply_exact_is_noop()
    {
        var stage = CreateStage();
        var fixture = AddFixture(stage);
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        var first = ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            []);
        stage.ClearDomainEvents();

        var second = ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            [..first.CreatedMatches]);

        second.CreatedMatches.Should().BeEmpty();
        fixture.MatchIds.Should().HaveCount(1);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_pairing_two_legs_creates_mirrored_matches_on_same_fixture()
    {
        var stage = CreateStage();
        var round = stage.AddRound("R1", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);

        var result = ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            []);

        result.CreatedMatches.Should().HaveCount(2);
        result.CreatedMatches[0].HomeEntryId.Should().Be(a);
        result.CreatedMatches[0].AwayEntryId.Should().Be(b);
        result.CreatedMatches[1].HomeEntryId.Should().Be(b);
        result.CreatedMatches[1].AwayEntryId.Should().Be(a);
        fixture.MatchIds.Should().HaveCount(2);
        fixture.Attachments.Should().HaveCount(2);
        fixture.Attachments.Single(attachment => attachment.LegIndex == 1).MatchId
            .Should().Be(result.CreatedMatches[0].Id);
        fixture.Attachments.Single(attachment => attachment.LegIndex == 2).MatchId
            .Should().Be(result.CreatedMatches[1].Id);
    }

    [Fact]
    public void Execute_pairing_two_legs_second_apply_exact_is_noop()
    {
        var stage = CreateStage();
        var round = stage.AddRound("R1", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        var first = ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            []);
        stage.ClearDomainEvents();

        var second = ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            [..first.CreatedMatches]);

        second.CreatedMatches.Should().BeEmpty();
        fixture.MatchIds.Should().HaveCount(2);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_pairing_two_legs_rejects_partial_leg1_only()
    {
        var stage = CreateStage();
        var round = stage.AddRound("R1", new TieFormat(TieFormat.TwoLegs, aggregateScoring: true), _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        var leg1Only = Match.Create(_competitionId, stage.Id, a, b, _clock);
        stage.AttachMatch(fixture.Id, leg1Only.Id, legIndex: 1, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            [leg1Only]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        fixture.MatchIds.Should().ContainSingle().Which.Should().Be(leg1Only.Id);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_pairing_rejects_partial_state()
    {
        var stage = CreateStage();
        var round = stage.AddRound("R1", _clock);
        var fixture1 = stage.AddFixture(round.Id, _clock);
        var fixture2 = stage.AddFixture(round.Id, _clock);
        var a = EntryId.New();
        var b = EntryId.New();
        var c = EntryId.New();
        var d = EntryId.New();
        var draw = PublishPairingDraw(
            stage,
            [new PairingDrawResult(a, b), new PairingDrawResult(c, d)],
            [a, b, c, d]);
        var onlyFirst = Match.Create(_competitionId, stage.Id, a, b, _clock);
        stage.AttachMatch(fixture1.Id, onlyFirst.Id, legIndex: 1, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext([fixture1.Id, fixture2.Id]),
            [onlyFirst]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        fixture1.MatchIds.Should().ContainSingle().Which.Should().Be(onlyFirst.Id);
        fixture2.MatchIds.Should().BeEmpty();
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Execute_pairing_rejects_divergent_match()
    {
        var stage = CreateStage();
        var fixture = AddFixture(stage);
        var a = EntryId.New();
        var b = EntryId.New();
        var c = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)], [a, b]);
        var divergent = Match.Create(_competitionId, stage.Id, a, c, _clock);
        stage.AttachMatch(fixture.Id, divergent.Id, legIndex: 1, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            [divergent]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        fixture.MatchIds.Should().ContainSingle().Which.Should().Be(divergent.Id);
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_pairing_rejects_inverted_home_away_as_divergent()
    {
        var stage = CreateStage();
        var fixture = AddFixture(stage);
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        var inverted = Match.Create(_competitionId, stage.Id, b, a, _clock);
        stage.AttachMatch(fixture.Id, inverted.Id, legIndex: 1, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            [inverted]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        fixture.MatchIds.Should().ContainSingle().Which.Should().Be(inverted.Id);
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_pairing_rejects_missing_fixture()
    {
        var stage = CreateStage();
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(FixtureId.New()),
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.DomainEvents.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_pairing_rejects_fixture_from_other_stage()
    {
        var stage = CreateStage();
        var other = CreateStage();
        var foreignFixture = AddFixture(other);
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(foreignFixture.Id),
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.DomainEvents.Should().BeEmpty();
        foreignFixture.MatchIds.Should().BeEmpty();
        draw.Status.Should().Be(DrawStatus.Published);
    }

    [Fact]
    public void Execute_pairing_rejects_incomplete_known_matches()
    {
        var stage = CreateStage();
        var fixture = AddFixture(stage);
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        var match = Match.Create(_competitionId, stage.Id, a, b, _clock);
        stage.AttachMatch(fixture.Id, match.Id, legIndex: 1, _clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        stage.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Record_pairing_rejects_entry_in_two_pairings_at_domain()
    {
        var stage = CreateStage();
        var a = EntryId.New();
        var b = EntryId.New();
        var c = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing([a, b, c]));

        var act = () => stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedPairings(
            [
                new PairingDrawResult(a, b),
                new PairingDrawResult(a, c)
            ]),
            _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.DrawResolutionInvalid);
    }

    [Fact]
    public void Execute_pairing_rejects_when_structure_locked()
    {
        var stage = CreateStage();
        var round = stage.AddRound("R1", _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        var a = EntryId.New();
        var b = EntryId.New();
        var draw = PublishPairingDraw(stage, [new PairingDrawResult(a, b)]);
        stage.Prepare(_clock);
        stage.Start(_clock);
        stage.ClearDomainEvents();

        var act = () => ApplyDraw.Execute(
            stage,
            draw.Id,
            _clock,
            new PairingApplicationContext(fixture.Id),
            []);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DrawApplyFailure);
        fixture.MatchIds.Should().BeEmpty();
        stage.DomainEvents.Should().BeEmpty();
    }

    private Stage CreateStage() =>
        Stage.Create(_competitionId, new StageName("Phase"), SampleRegulations.Standard(), _clock);

    private Fixture AddFixture(Stage stage)
    {
        var round = stage.AddRound("R1", _clock);
        return stage.AddFixture(round.Id, _clock);
    }

    private (Draw Draw, EntryId Entry) PublishSlotDraw(Stage stage, string slotKey)
    {
        stage.AddSlot(slotKey);
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, slotKey)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        return (draw, entry);
    }

    private Draw PublishGroupDraw(Stage stage, EntryId entry, GroupId groupId)
    {
        var draw = stage.CreateDraw(DrawResolutionKind.Group, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForGroup([entry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedGroups([new GroupDrawPlacement(entry, groupId)]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);
        return draw;
    }

    private Draw PublishPairingDraw(
        Stage stage,
        IReadOnlyList<PairingDrawResult> pairings,
        IReadOnlyList<EntryId>? pool = null)
    {
        var entries = pool ?? [..pairings.SelectMany(p => new[] { p.EntryA, p.EntryB }).Distinct()];
        var draw = stage.CreateDraw(DrawResolutionKind.Pairing, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForPairing(entries));
        stage.RecordDrawResolution(draw.Id, DrawResolution.ResolvedPairings(pairings), _clock);
        stage.PublishDraw(draw.Id, _clock);
        return draw;
    }
}
