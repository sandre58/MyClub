// -----------------------------------------------------------------------
// <copyright file="PrepareStageTests.cs" company="Stéphane ANDRE">
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

public sealed class PrepareStageTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 19, 30, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_prepares_stage_without_slots_without_whofeeds()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("QF", _clock);

        var resolutions = PrepareStage.Execute(stage, [stage], _clock);

        resolutions.Should().BeEmpty();
        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Execute_prepares_when_all_slots_have_unique_feeds()
    {
        var competitionId = CompetitionId.New();
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");
        var a = EntryId.New();
        var b = EntryId.New();
        target.ReplaceCompositionEntries([a, b], _clock);
        target.AssignEntryToSlot("SF1-A", a);
        target.AssignEntryToSlot("SF1-B", b);

        var resolutions = PrepareStage.Execute(target, [target], _clock);

        resolutions.Should().HaveCount(2);
        resolutions.Should().OnlyContain(r => r.Status == FeedResolutionStatus.Unique);
        target.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Execute_rejects_missing_feed_and_does_not_prepare()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Knockout"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddRound("QF", _clock);
        stage.AddSlot("SF1-A");

        var act = () => PrepareStage.Execute(stage, [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SlotFeedsInvalid);
        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Execute_rejects_multiple_feeds()
    {
        var competitionId = CompetitionId.New();
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A");
        var directEntry = EntryId.New();
        target.ReplaceCompositionEntries([directEntry], _clock);
        target.AssignEntryToSlot("SF1-A", directEntry);
        var drawEntry = EntryId.New();
        var draw = target.CreateDraw(DrawResolutionKind.Slot, _clock);
        target.ConfigureDrawInputs(draw.Id, DrawInputs.ForSlot([drawEntry]));
        target.RecordDrawResolution(
            draw.Id,
            DrawResolution.ResolvedSlots([new SlotDrawPlacement(drawEntry, "SF1-A")]),
            _clock);
        target.PublishDraw(draw.Id, _clock);

        var act = () => PrepareStage.Execute(target, [target], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SlotFeedsInvalid);
        target.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Execute_prepares_when_progression_references_round_without_tie_format()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QuarterFinal", tieFormat: null, _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.AddSlot("SF1-A");
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fixture.Id.Value.ToString("N"),
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        PrepareStage.Execute(stage, [stage], _clock);

        stage.Status.Should().Be(StageStatus.Ready);
        round.TieFormat.Should().BeNull();
    }

    [Fact]
    public void Execute_prepares_when_progression_references_round_with_tie_format()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound(
            "QuarterFinal",
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            _clock);
        var fixture = stage.AddFixture(round.Id, _clock);
        stage.AddSlot("SF1-A");
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fixture.Id.Value.ToString("N"),
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(stage.Id, "SF1-A"))
            ]),
            _clock);

        PrepareStage.Execute(stage, [stage], _clock);

        stage.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Execute_prepares_when_round_has_no_tie_format_and_no_progression()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var round = stage.AddRound("QuarterFinal", tieFormat: null, _clock);
        stage.AddFixture(round.Id, _clock);

        PrepareStage.Execute(stage, [stage], _clock);

        stage.Status.Should().Be(StageStatus.Ready);
        round.TieFormat.Should().BeNull();
    }

    [Fact]
    public void ReplaceProgressionRules_allows_cross_stage_place_destination()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var semi = Stage.Create(competitionId, new StageName("SemiFinal"), SampleRegulations.Standard(), _clock);
        var round = source.AddRound(
            "QuarterFinal",
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            _clock);
        var fixture = source.AddFixture(round.Id, _clock);
        semi.AddRound("SF", _clock);
        semi.AddSlot("SF1-A");
        semi.AddSlot("SF1B");

        var act = () => source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fixture.Id.Value.ToString("N"),
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(semi.Id, "SF1-B"))
            ]),
            _clock);

        act.Should().NotThrow();
        source.Regulation.ProgressionRules!.Paths.Should().ContainSingle()
            .Which.Destination.Should().Be(new ProgressionDestination(semi.Id, "SF1-B"));
    }

    [Fact]
    public void ReplaceQualificationRules_rejects_population_destination_targeting_self()
    {
        var competitionId = CompetitionId.New();
        var groups = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var group = groups.AddGroup("A", _clock);
        groups.AssignEntryToGroup(group.Id, EntryId.New());
        groups.AddMatchday(1, _clock);

        var act = () => groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(groups.Id))
            ]),
            _clock);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Execute_rejects_progression_outbound_when_destination_stage_missing()
    {
        var competitionId = CompetitionId.New();
        var groupStage = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var semi = Stage.Create(competitionId, new StageName("SemiFinal"), SampleRegulations.Standard(), _clock);
        var final = Stage.Create(competitionId, new StageName("Final"), SampleRegulations.Standard(), _clock);
        var superFinalId = StageId.New();

        var round = final.AddRound(
            "Final",
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            _clock);
        var fixture = final.AddFixture(round.Id, _clock);
        final.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fixture.Id.Value.ToString("N"),
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(superFinalId))
            ]),
            _clock);

        var act = () => PrepareStage.Execute(final, [groupStage, semi, final], _clock);

        var ex = act.Should().Throw<ApplicationFailureException>().Which;
        ex.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
        ex.Message.Should().Contain(superFinalId.ToString());
        final.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Execute_rejects_qualification_outbound_when_destination_stage_missing()
    {
        var competitionId = CompetitionId.New();
        var groups = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var semi = Stage.Create(competitionId, new StageName("Semi"), SampleRegulations.Standard(), _clock);
        var ghostId = StageId.New();
        var group = groups.AddGroup("A", _clock);
        groups.AssignEntryToGroup(group.Id, EntryId.New());
        groups.AddMatchday(1, _clock);
        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(ghostId))
            ]),
            _clock);

        var act = () => PrepareStage.Execute(groups, [groups, semi], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StageNotInCompetition);
        groups.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Execute_prepares_when_progression_outbound_destination_is_valid()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("QF"), SampleRegulations.Standard(), _clock);
        var semi = Stage.Create(competitionId, new StageName("SemiFinal"), SampleRegulations.Standard(), _clock);
        var round = source.AddRound(
            "QuarterFinal",
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false),
            _clock);
        var fixture = source.AddFixture(round.Id, _clock);
        semi.AddRound("SF", _clock);
        semi.AddSlot("SF1-A");
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(fixture.Id.Value.ToString("N"),
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForPopulation(semi.Id))
            ]),
            _clock);

        PrepareStage.Execute(source, [source, semi], _clock);

        source.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public void Execute_prepares_when_qualification_outbound_destination_is_valid()
    {
        var competitionId = CompetitionId.New();
        var groups = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var semi = Stage.Create(competitionId, new StageName("SemiFinal"), SampleRegulations.Standard(), _clock);
        var group = groups.AddGroup("A", _clock);
        groups.AssignEntryToGroup(group.Id, EntryId.New());
        groups.AddMatchday(1, _clock);
        semi.AddRound("SF", _clock);
        semi.AddSlot("SF1-A");
        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(semi.Id))
            ]),
            _clock);

        PrepareStage.Execute(groups, [groups, semi], _clock);

        groups.Status.Should().Be(StageStatus.Ready);
    }
}
