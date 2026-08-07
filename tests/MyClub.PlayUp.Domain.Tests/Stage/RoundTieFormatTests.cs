// -----------------------------------------------------------------------
// <copyright file="RoundTieFormatTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Stage.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Domain.Tests.Stage;

public sealed class RoundTieFormatTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly CompetitionId _competitionId = CompetitionId.New();

    [Fact]
    public void AddRound_without_stage_default_has_null_tie_format()
    {
        // Arrange
        var stage = CreateCupStage(tieFormatDefault: null);

        // Act
        var round = stage.AddRound("Final", _clock);

        // Assert
        round.TieFormat.Should().BeNull();
    }

    [Fact]
    public void AddRound_materializes_stage_default_tie_format()
    {
        // Arrange
        var defaultFormat = new TieFormat(2, true, new AwayGoalsRule());
        var stage = CreateCupStage(defaultFormat);

        // Act
        var round = stage.AddRound("QF", _clock);

        // Assert
        round.TieFormat.Should().Be(defaultFormat);
        ReferenceEquals(round.TieFormat, stage.Regulation.TieFormat).Should().BeFalse();
        ReferenceEquals(round.TieFormat!.AwayGoalsRule, stage.Regulation.TieFormat!.AwayGoalsRule)
            .Should().BeFalse();
    }

    [Fact]
    public void AddRound_with_explicit_tie_format_overrides_default()
    {
        // Arrange
        var defaultFormat = new TieFormat(2, true);
        var finalFormat = new TieFormat(
            1,
            false,
            extraTimeRule: new ExtraTimeRule(),
            penaltyShootoutRule: new PenaltyShootoutRule());
        var stage = CreateCupStage(defaultFormat);

        // Act
        var round = stage.AddRound("Final", finalFormat, _clock);

        // Assert
        round.TieFormat.Should().Be(finalFormat);
        stage.Regulation.TieFormat.Should().Be(defaultFormat);
    }

    [Fact]
    public void Round_tie_format_stays_unchanged_when_stage_default_is_replaced()
    {
        // Arrange
        var initialDefault = new TieFormat(2, true, new AwayGoalsRule());
        var stage = CreateCupStage(initialDefault);
        var round = stage.AddRound("QF", _clock);
        var roundSnapshot = round.TieFormat;

        // Act
        stage.ReplaceRegulation(
            stage.Regulation.WithTieFormat(new TieFormat(1, false)),
            _clock);

        // Assert
        stage.Regulation.TieFormat!.NumberOfLegs.Should().Be(1);
        round.TieFormat.Should().Be(roundSnapshot);
        round.TieFormat!.NumberOfLegs.Should().Be(2);
    }

    [Fact]
    public void ReplaceRoundTieFormat_updates_round_only()
    {
        // Arrange
        var stage = CreateCupStage(new TieFormat(2, true));
        var round = stage.AddRound("QF", _clock);
        stage.ClearDomainEvents();
        var replacement = new TieFormat(1, false, extraTimeRule: new ExtraTimeRule());

        // Act
        stage.ReplaceRoundTieFormat(round.Id, replacement, _clock);

        // Assert
        round.TieFormat.Should().Be(replacement);
        stage.Regulation.TieFormat!.NumberOfLegs.Should().Be(2);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageRoundTieFormatReplaced>();
    }

    [Fact]
    public void ReplaceRoundTieFormat_after_Start_is_rejected()
    {
        // Arrange
        var stage = CreateCupStage(new TieFormat(2, true));
        var round = stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);

        // Act
        var act = () => stage.ReplaceRoundTieFormat(round.Id, new TieFormat(1, false), _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
    }

    private StageAggregate CreateCupStage(TieFormat? tieFormatDefault)
    {
        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            SampleRegulations.Standard().StandingRules,
            tieFormatDefault);
        return StageAggregate.Create(_competitionId, new StageName("Cup"), regulation, _clock);
    }
}
