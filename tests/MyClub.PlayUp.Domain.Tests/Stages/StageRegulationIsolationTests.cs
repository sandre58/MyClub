// -----------------------------------------------------------------------
// <copyright file="StageRegulationIsolationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Stages.Events;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageRegulationIsolationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Create_materializes_regulation_from_competition()
    {
        // Arrange
        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);

        // Act
        var stage = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            competition.Regulation,
            _clock);

        // Assert
        stage.Regulation.Should().NotBeNull();
        stage.Regulation.MatchRules.Should().Be(competition.Regulation.MatchRules);
        stage.Regulation.StandingRules.Should().Be(competition.Regulation.StandingRules);
        ReferenceEquals(stage.Regulation.MatchRules, competition.Regulation.MatchRules).Should().BeFalse();
        ReferenceEquals(stage.Regulation.StandingRules, competition.Regulation.StandingRules).Should().BeFalse();
    }

    [Fact]
    public void Stage_regulation_stays_unchanged_when_competition_regulation_is_replaced()
    {
        // Arrange
        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            competition.Regulation,
            _clock);
        var stageSnapshot = stage.Regulation;

        var replacement = new Regulation(
            new EntryRules(4, 8),
            new MatchRules(
                new MatchDuration(40, 2, 10),
                new AdministrativeResultPolicy(2, 0)),
            competition.Regulation.StandingRules);

        // Act
        competition.ReplaceRegulation(replacement, _clock);

        // Assert
        competition.Regulation.Should().Be(replacement);
        stage.Regulation.Should().Be(stageSnapshot);
        stage.Regulation.MatchRules.Duration.DurationPerPeriod.Should().Be(45);
        competition.Regulation.MatchRules.Duration.DurationPerPeriod.Should().Be(40);
    }

    [Fact]
    public void Replacing_stage_regulation_does_not_change_competition()
    {
        // Arrange
        var competition = Competition.Create(
            new CompetitionName("League"),
            SampleRegulations.Standard(),
            _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            competition.Regulation,
            _clock);
        var competitionSnapshot = competition.Regulation;
        var customized = new StageRegulation(
            new MatchRules(
                new MatchDuration(30, 2, 5),
                new AdministrativeResultPolicy(3, 0),
                new ExtraTimePolicy(10, 2)),
            competition.Regulation.StandingRules);

        // Act
        stage.ReplaceRegulation(customized, _clock);

        // Assert
        competition.Regulation.Should().Be(competitionSnapshot);
        stage.Regulation.MatchRules.Duration.DurationPerPeriod.Should().Be(30);
        stage.Regulation.MatchRules.ExtraTimePolicy.Should().NotBeNull();
        competition.Regulation.MatchRules.ExtraTimePolicy.Should().BeNull();
    }

    [Fact]
    public void ReplaceRegulation_on_Ready_demotes_to_Draft()
    {
        // Arrange
        var stage = CreateReady();
        stage.ClearDomainEvents();
        var replacement = StageRegulation.MaterializeFrom(SampleRegulations.Standard());

        // Act
        stage.ReplaceRegulation(replacement, _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Draft);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageRegulationReplaced>();
    }

    [Fact]
    public void ReplaceRegulation_after_Start_is_rejected()
    {
        // Arrange
        var stage = CreateRunning();

        // Act
        var act = () => stage.ReplaceRegulation(
            StageRegulation.MaterializeFrom(SampleRegulations.Standard()),
            _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    [Fact]
    public void ReplaceStandingRules_is_allowed_after_Start()
    {
        // Arrange
        var stage = CreateRunning();
        stage.ClearDomainEvents();
        var previousMatchRules = stage.Regulation.MatchRules;
        var newStanding = new StandingRules(
            new PointsPolicy(2, 1, 0),
            [RankingCriterion.Points, RankingCriterion.Wins]);

        // Act
        stage.ReplaceStandingRules(newStanding, _clock);

        // Assert
        stage.Status.Should().Be(StageStatus.Running);
        stage.Regulation.StandingRules.Should().Be(newStanding);
        stage.Regulation.MatchRules.Should().Be(previousMatchRules);
        stage.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<StageStandingRulesReplaced>();
    }

    [Fact]
    public void ReplaceStandingRules_on_Completed_is_rejected()
    {
        // Arrange
        var stage = CreateRunning();
        stage.Complete(_clock);

        // Act
        var act = () => stage.ReplaceStandingRules(
            new StandingRules(new PointsPolicy(3, 1, 0), [RankingCriterion.Points]),
            _clock);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.InvalidTransition);
    }

    private Stage CreateReady()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("League"),
            SampleRegulations.Standard(),
            _clock);
        stage.AddMatchday(1, _clock);
        stage.Prepare(_clock);
        return stage;
    }

    private Stage CreateRunning()
    {
        var stage = CreateReady();
        stage.Start(_clock);
        return stage;
    }
}
