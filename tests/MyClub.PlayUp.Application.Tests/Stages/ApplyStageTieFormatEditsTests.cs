// -----------------------------------------------------------------------
// <copyright file="ApplyStageTieFormatEditsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Stages.ApplyStageTieFormatEdits;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ApplyStageTieFormatEditsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 27, 20, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Execute_from_Ready_reverts_once_then_applies_default_and_round_edits()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stageRegulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            standingRules: null,
            new TieFormat(TieFormat.SingleLeg, aggregateScoring: false));
        var stage = Stage.Create(competition.Id, new StageName("Cup"), stageRegulation, _clock);
        competition.AddStage(stage.Id, _clock);
        var round = stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Status.Should().Be(StageStatus.Ready);

        var newDefault = new TieFormat(TieFormat.TwoLegs, aggregateScoring: true);
        var roundEdit = new TieFormat(TieFormat.SingleLeg, aggregateScoring: false, extraTimeRule: new ExtraTimeRule());

        ApplyStageTieFormatEdits.Execute(
            stage,
            competition,
            newDefault,
            replaceDefault: true,
            [new StageTieFormatRoundEdit(round.Id, roundEdit)],
            stageMatches: [],
            _clock);

        stage.Status.Should().Be(StageStatus.Draft);
        stage.Regulation.TieFormat.Should().Be(newDefault);
        round.TieFormat.Should().Be(roundEdit);
        stage.DomainEvents.Count(e => e is StageRevertedToDraft).Should().Be(1);
    }

    [Fact]
    public void Execute_rolls_back_all_edits_when_second_round_edit_fails()
    {
        var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
        var stage = Stage.Create(competition.Id, new StageName("Cup"), SampleRegulations.Standard(), _clock);
        competition.AddStage(stage.Id, _clock);
        var round = stage.AddRound("QF", _clock);
        var originalDefault = stage.Regulation.TieFormat;

        var act = () => ApplyStageTieFormatEdits.Execute(
            stage,
            competition,
            new TieFormat(TieFormat.TwoLegs, aggregateScoring: true),
            replaceDefault: true,
            [
                new StageTieFormatRoundEdit(round.Id, new TieFormat(TieFormat.SingleLeg, aggregateScoring: false)),
                new StageTieFormatRoundEdit(RoundId.New(), new TieFormat(TieFormat.SingleLeg, aggregateScoring: false)),
            ],
            stageMatches: [],
            _clock);

        act.Should().Throw<DomainException>();
        stage.Regulation.TieFormat.Should().Be(originalDefault);
        round.TieFormat.Should().BeNull();
    }
}
