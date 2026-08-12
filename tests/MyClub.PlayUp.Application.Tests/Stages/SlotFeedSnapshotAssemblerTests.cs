// -----------------------------------------------------------------------
// <copyright file="SlotFeedSnapshotAssemblerTests.cs" company="Stéphane ANDRE">
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

public sealed class SlotFeedSnapshotAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 19, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_maps_qualification_from_source_stage_to_target_slot()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A", _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 1),
                    new QualificationDestination(target.Id, "SF1-A"))
            ]),
            _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        snapshot.InboundQualification.Should().ContainSingle();
        snapshot.InboundQualification[0].SourceStageId.Should().Be(source.Id);
        snapshot.InboundQualification[0].DestinationSlotKey.Should().Be("SF1-A");
        snapshot.DrawTargets.Should().BeEmpty();
    }

    [Fact]
    public void Assemble_maps_conditional_qualification_path_as_feed()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A", _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 3),
                    new QualificationDestination(target.Id, "SF1-A"),
                    QualificationCondition.PointsAtLeast(40))
            ]),
            _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        snapshot.InboundQualification.Should().ContainSingle();
        snapshot.InboundQualification[0].SourceStageId.Should().Be(source.Id);
        snapshot.InboundQualification[0].DestinationSlotKey.Should().Be("SF1-A");
    }

    [Fact]
    public void Assemble_maps_cross_stage_progression()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        var round = source.AddRound("QF", _clock);
        var fixture = source.AddFixture(round.Id, _clock);
        target.AddRound("SF", _clock);
        target.AddSlot("SF1-A", _clock);

        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(target.Id, "SF1-A"))
            ]),
            _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        snapshot.InboundProgression.Should().ContainSingle();
        snapshot.InboundProgression[0].SourceFixtureId.Should().Be(fixture.Id);
        snapshot.InboundProgression[0].SourceStageId.Should().Be(source.Id);
    }

    [Fact]
    public void Assemble_rejects_dangling_slot_target()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A", _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 1),
                    new QualificationDestination(target.Id, "Missing"))
            ]),
            _clock);

        var act = () => SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DanglingFeedTarget);
    }

    [Fact]
    public void Assemble_rejects_target_not_in_list()
    {
        var competitionId = CompetitionId.New();
        var other = Stage.Create(competitionId, new StageName("A"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("B"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A", _clock);

        var act = () => SlotFeedSnapshotAssembler.Assemble(target, [other]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
    }

    [Fact]
    public void Assemble_exposes_DrawTargets_only_after_Publish_Slot_draw()
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("SF1-A", _clock);
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(entry, "SF1-A")]),
            _clock);

        SlotFeedSnapshotAssembler.Assemble(stage, [stage]).DrawTargets.Should().BeEmpty();

        stage.PublishDraw(draw.Id, _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, [stage]);
        snapshot.DrawTargets.Should().ContainSingle();
        snapshot.DrawTargets[0].DrawId.Should().Be(draw.Id);
        snapshot.DrawTargets[0].SlotKey.Should().Be("SF1-A");
    }

    [Fact]
    public void Assemble_Direct_plus_published_Draw_is_MultipleFeeds_for_resolver()
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("SF1-A", _clock);
        var directEntry = EntryId.New();
        stage.AssignEntryToSlot("SF1-A", directEntry, _clock);
        var drawEntry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([drawEntry]), _clock);
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(drawEntry, "SF1-A")]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, [stage]);
        SlotFeedResolver.Resolve(snapshot, "SF1-A").Status.Should().Be(FeedResolutionStatus.MultipleFeeds);
    }
}
