// -----------------------------------------------------------------------
// <copyright file="PrepareStageTests.cs" company="Stéphane ANDRE">
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

public sealed class PrepareStageTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 9, 19, 30, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_prepares_stage_without_slots_without_whofeeds()
    {
        var stage = StageAggregate.Create(
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
        var source = StageAggregate.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = StageAggregate.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
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
        var stage = StageAggregate.Create(
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
        var source = StageAggregate.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = StageAggregate.Create(competitionId, new StageName("Knockout"), SampleRegulations.Standard(), _clock);
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
}
