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
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A", _clock);
        target.AddSlot("SF1-B", _clock);
        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 1),
                    new QualificationDestination(target.Id, "SF1-A")),
                new QualificationPath(
                    2,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Top, 2),
                    new QualificationDestination(target.Id, "SF1-B"))
            ]),
            _clock);

        var resolutions = PrepareStage.Execute(target, [source, target], _clock);

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
        stage.AddSlot("SF1-A", _clock);

        var act = () => PrepareStage.Execute(stage, [stage], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.SlotFeedsInvalid);
        stage.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Execute_rejects_multiple_feeds()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
        target.AddRound("QF", _clock);
        target.AddSlot("SF1-A", _clock);
        target.AssignEntryToSlot("SF1-A", EntryId.New(), _clock);
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

        var act = () => PrepareStage.Execute(target, [source, target], _clock);

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
        stage.AddSlot("SF1-A", _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
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
        stage.AddSlot("SF1-A", _clock);
        stage.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
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
    public void Execute_rejects_progression_outbound_when_destination_slot_missing()
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
        semi.AddSlot("SF1-A", _clock);
        semi.AddSlot("SF1B", _clock);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(semi.Id, "SF1-B"))
            ]),
            _clock);

        var act = () => PrepareStage.Execute(source, [source, semi], _clock);

        var ex = act.Should().Throw<ApplicationFailureException>().Which;
        ex.Code.Should().Be(ApplicationErrorCodes.DanglingFeedTarget);
        ex.Message.Should().Contain("SF1-B");
        source.Status.Should().Be(StageStatus.Draft);
        semi.Status.Should().Be(StageStatus.Draft);
    }

    [Fact]
    public void Execute_rejects_qualification_outbound_when_destination_slot_missing()
    {
        var competitionId = CompetitionId.New();
        var groups = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var semi = Stage.Create(competitionId, new StageName("SemiFinal"), SampleRegulations.Standard(), _clock);
        var group = groups.AddGroup("A", _clock);
        groups.AssignEntryToGroup(group.Id, EntryId.New());
        groups.AddMatchday(1, _clock);
        semi.AddRound("SF", _clock);
        semi.AddSlot("SF1-A", _clock);
        semi.AddSlot("SF1B", _clock);
        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(semi.Id, "SF1-B"))
            ]),
            _clock);

        var act = () => PrepareStage.Execute(groups, [groups, semi], _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DanglingFeedTarget);
        groups.Status.Should().Be(StageStatus.Draft);
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
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(superFinalId, "Champ"))
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
                    new QualificationDestination(ghostId, "SF1-A"))
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
        semi.AddSlot("SF1-A", _clock);
        source.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    fixture.Id,
                    ProgressionOutcome.Winner,
                    new ProgressionDestination(semi.Id, "SF1-A"))
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
        semi.AddSlot("SF1-A", _clock);
        groups.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    new QualificationDestination(semi.Id, "SF1-A"))
            ]),
            _clock);

        PrepareStage.Execute(groups, [groups, semi], _clock);

        groups.Status.Should().Be(StageStatus.Ready);
    }
}
