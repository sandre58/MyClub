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
    public void Assemble_emits_qualification_place_as_slot_feed()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");

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

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        snapshot.InboundQualification.Should().ContainSingle();
        snapshot.InboundQualification[0].SourceStageId.Should().Be(source.Id);
        snapshot.InboundQualification[0].DestinationSlotKey.Should().Be("SF1-A");
        SlotFeedResolver.Resolve(snapshot, "SF1-A").Status.Should().Be(FeedResolutionStatus.Unique);
    }

    [Fact]
    public void Assemble_Qual_place_plus_Direct_is_MultipleFeeds()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");
        var directEntry = EntryId.New();
        target.ReplaceCompositionEntries([directEntry], _clock);
        target.AssignEntryToSlot("SF1-A", directEntry);

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

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);
        SlotFeedResolver.Resolve(snapshot, "SF1-A").Status.Should().Be(FeedResolutionStatus.MultipleFeeds);
    }

    [Fact]
    public void Assemble_does_not_emit_qualification_population_as_slot_feeds()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 1),
                    QualificationDestination.ForPopulation(target.Id))
            ]),
            _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        snapshot.InboundQualification.Should().BeEmpty();
        snapshot.DrawTargets.Should().BeEmpty();
    }

    [Fact]
    public void Assemble_ignores_conditional_qualification_population_paths()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 3),
                    QualificationDestination.ForPopulation(target.Id),
                    QualificationCondition.PointsAtLeast(40))
            ]),
            _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        snapshot.InboundQualification.Should().BeEmpty();
    }

    [Fact]
    public void Assemble_emits_progression_WhoFeeds_on_PairKey_without_fixtures()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        source.AddRound("Tour", new TieFormat(TieFormat.SingleLeg, aggregateScoring: false), _clock);
        source.AddSlot("S1");
        source.AddSlot("S2");
        source.AddSlot("S3");
        source.AddSlot("S4");
        source.SeedEntryRoundBracketPairs();
        source.Rounds[0].Fixtures.Should().BeEmpty();
        target.AddRound("SF", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");

        ReplaceStageProgressionRules.Execute(
            source,
            [
                new ProgressionIntentSpec(
                    IntentId: null,
                    Order: 1,
                    RoundId: source.Rounds[0].Id,
                    Outcome: ProgressionOutcome.Winner,
                    DestinationStageId: target.Id,
                    DestinationSlotKeys: ["SF1-A", "SF1-B"])
            ],
            _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        snapshot.InboundProgression.Should().HaveCount(2);
        snapshot.InboundProgression.Select(p => p.SourcePairKey).Should().Equal("P1", "P2");
        SlotFeedResolver.Resolve(snapshot, "SF1-A").Source!.Progression!.SourcePairKey.Should().Be("P1");
        SlotFeedResolver.Resolve(snapshot, "SF1-B").Source!.Progression!.SourcePairKey.Should().Be("P2");
    }

    [Fact]
    public void Assemble_ignores_cross_stage_population_progression()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
        var round = source.AddRound("QF", _clock);
        source.AddSlot("KO-A");
        source.AddSlot("KO-B");
        source.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        _ = source.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");
        target.AddRound("SF", _clock);
        target.AddSlot("SF1-A");

        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1",
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(target.Id))
            ]),
            _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(target, [source, target]);

        snapshot.InboundProgression.Should().BeEmpty();
        snapshot.DrawTargets.Should().BeEmpty();
    }

    [Fact]
    public void Assemble_rejects_dangling_same_stage_progression_slot_target()
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(competitionId, new StageName("KO"), SampleRegulations.Standard(), _clock);
        var round = stage.AddRound("R1", _clock);
        stage.AddSlot("KO-A");
        stage.AddSlot("KO-B");
        stage.AddSlot("SF1-A");
        stage.ReplaceBracketPairs([new BracketPair("P1", "KO-A", "KO-B")]);
        _ = stage.AddFixture(round.Id, _clock, "KO-A", "KO-B", "P1");

        var act = () => stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath("P1",
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "Missing"))
            ]),
            _clock);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(StageErrorCodes.SlotNotFound);
    }

    [Fact]
    public void Assemble_rejects_target_not_in_list()
    {
        var competitionId = CompetitionId.New();
        var other = Stage.Create(competitionId, new StageName("A"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("B"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");

        var act = () => SlotFeedSnapshotAssembler.Assemble(target, [other]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
    }

    [Fact]
    public void Assemble_exposes_DrawTargets_only_after_Publish_Slot_draw()
    {
        var competitionId = CompetitionId.New();
        var stage = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        stage.AddSlot("SF1-A");
        var entry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([entry]));
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
        stage.AddSlot("SF1-A");
        var directEntry = EntryId.New();
        stage.ReplaceCompositionEntries([directEntry], _clock);
        stage.AssignEntryToSlot("SF1-A", directEntry);
        var drawEntry = EntryId.New();
        var draw = stage.CreateDraw(DrawResolutionKind.Slot, _clock);
        stage.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([drawEntry]));
        stage.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(drawEntry, "SF1-A")]),
            _clock);
        stage.PublishDraw(draw.Id, _clock);

        var snapshot = SlotFeedSnapshotAssembler.Assemble(stage, [stage]);
        SlotFeedResolver.Resolve(snapshot, "SF1-A").Status.Should().Be(FeedResolutionStatus.MultipleFeeds);
    }
}
