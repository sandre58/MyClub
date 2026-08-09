// -----------------------------------------------------------------------
// <copyright file="SlotFeedSnapshotAssemblerTests.cs" company="Stéphane ANDRE">
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

public sealed class SlotFeedSnapshotAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 19, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_maps_qualification_from_source_stage_to_target_slot()
    {
        var competitionId = CompetitionId.New();
        var source = StageAggregate.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = StageAggregate.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
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
    public void Assemble_maps_cross_stage_progression()
    {
        var competitionId = CompetitionId.New();
        var source = StageAggregate.Create(competitionId, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var target = StageAggregate.Create(competitionId, new StageName("SF"), SampleRegulations.Standard(), _clock);
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
        var source = StageAggregate.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = StageAggregate.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
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
        var other = StageAggregate.Create(competitionId, new StageName("A"), SampleRegulations.Standard(), _clock);
        var target = StageAggregate.Create(competitionId, new StageName("B"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A", _clock);

        var act = () => SlotFeedSnapshotAssembler.Assemble(target, [other]);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
    }
}
