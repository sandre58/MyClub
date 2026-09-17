// -----------------------------------------------------------------------
// <copyright file="RegulationIntegrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class RegulationIntegrationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 7, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Competition_create_and_replace_preserves_independent_regulation()
    {
        // Arrange
        var regulation = SampleRegulations.Standard();
        var competition = Competition.Create(new CompetitionName("Cup"), regulation, _clock);

        // Assert create
        competition.Regulation.Should().Be(regulation);
        ReferenceEquals(competition.Regulation, regulation).Should().BeFalse();
        competition.Regulation.EntryRules.Should().NotBeNull();
        competition.Regulation.MatchRules.Should().NotBeNull();
        competition.Regulation.StandingRules.Should().NotBeNull();

        // Act replace
        var replacement = new Regulation(
            new EntryRules(4, 16),
            regulation.MatchRules,
            regulation.StandingRules);
        competition.ReplaceRegulation(replacement, _clock);

        // Assert
        competition.Regulation.EntryRules.MinimumTeams.Should().Be(4);
        ReferenceEquals(competition.Regulation, replacement).Should().BeFalse();
    }

    [Fact]
    public void Stage_materializes_from_competition_without_runtime_fallback()
    {
        // Arrange
        var competition = Competition.Create(
            new CompetitionName("Cup"),
            SampleRegulations.Standard(),
            _clock);

        // Act
        var stage = Stage.Create(
            competition.Id,
            new StageName("Poules"),
            competition.Regulation,
            _clock);

        // Assert
        stage.Regulation.MatchRules.Should().Be(competition.Regulation.MatchRules);
        stage.Regulation.StandingRules.Should().Be(competition.Regulation.StandingRules);
        stage.Regulation.DrawRules.Should().BeNull();
        stage.Regulation.QualificationRules.Should().BeNull();
        stage.Regulation.TieFormat.Should().BeNull();
        ReferenceEquals(stage.Regulation.MatchRules, competition.Regulation.MatchRules).Should().BeFalse();
    }

    [Fact]
    public void Stage_full_regulation_composition_is_independent_of_competition()
    {
        // Arrange
        var competition = Competition.Create(
            new CompetitionName("Cup"),
            SampleRegulations.Standard(),
            _clock);
        var defaultTie = new TieFormat(2, true, new AwayGoalsRule());
        var draw = new DrawRules(DrawMode.Random, new SeedingRules(4));
        var qualification = new QualificationRules(
        [
            new QualificationPath(
                1,
                QualificationSource.Overall(),
                new QualificationSelection(SelectionMode.Top, 2),
                QualificationDestination.ForPopulation(StageId.New()))
        ]);
        var stageRegulation = new StageRegulation(
            competition.Regulation.MatchRules,
            competition.Regulation.StandingRules,
            defaultTie,
            draw,
            qualification);

        // Act
        var stage = Stage.Create(competition.Id, new StageName("Poules"), stageRegulation, _clock);
        competition.ReplaceRegulation(
            new Regulation(new EntryRules(8, 32), competition.Regulation.MatchRules, competition.Regulation.StandingRules),
            _clock);
        stage.ReplaceDrawRules(new DrawRules(DrawMode.Random, new SeedingRules(2)), _clock);

        // Assert
        competition.Regulation.EntryRules.MinimumTeams.Should().Be(8);
        stage.Regulation.DrawRules!.Mode.Should().Be(DrawMode.Random);
        stage.Regulation.DrawRules.SeedingRules!.NumberOfSeeds.Should().Be(2);
        stage.Regulation.QualificationRules.Should().Be(qualification);
        stage.Regulation.TieFormat.Should().Be(defaultTie);
        stage.Regulation.MatchRules.Duration.DurationPerPeriod.Should().Be(45);
    }

    [Fact]
    public void Round_materializes_tie_format_and_has_no_match_rules()
    {
        // Arrange
        var defaultTie = new TieFormat(2, true);
        var finalTie = new TieFormat(1, false, extraTimeRule: new ExtraTimeRule());
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            new StageRegulation(
                SampleRegulations.Standard().MatchRules,
                standingRules: null,
                defaultTie),
            _clock);

        // Act
        var quarter = stage.AddRound("QF", _clock);
        var final = stage.AddRound("Final", finalTie, _clock);

        // Assert
        quarter.TieFormat.Should().Be(defaultTie);
        final.TieFormat.Should().Be(finalTie);
        ReferenceEquals(quarter.TieFormat, stage.Regulation.TieFormat).Should().BeFalse();
        typeof(Round).GetProperty("MatchRules").Should().BeNull();
        typeof(Round).GetProperty(nameof(Round.TieFormat)).Should().NotBeNull();
    }

    [Fact]
    public void After_Start_structural_rules_are_frozen_and_standing_remains_mutable_on_classifying_stage()
    {
        // Arrange — Cup (non-classifying): structure locked; standing absent / not replaceable
        var cup = CreateRunningCupStage();
        var replaceRegulation = () => cup.ReplaceRegulation(
            StageRegulation.MaterializeFrom(SampleRegulations.Standard(), isClassifyingPhase: false),
            _clock);
        var replaceDraw = () => cup.ReplaceDrawRules(new DrawRules(DrawMode.Random), _clock);
        var replaceQual = () => cup.ReplaceQualificationRules(null, _clock);
        var replaceDefaultTie = () => cup.ReplaceDefaultTieFormat(new TieFormat(1, false), _clock);
        var replaceRoundTie = () => cup.ReplaceRoundTieFormat(cup.Rounds[0].Id, new TieFormat(1, false), _clock);
        var replaceStandingOnCup = () => cup.ReplaceStandingRules(
            new StandingRules(new PointsPolicy(2, 1, 0), [RankingCriterion.Points]),
            _clock);

        replaceRegulation.Should().Throw<DomainException>();
        replaceDraw.Should().Throw<DomainException>();
        replaceQual.Should().Throw<DomainException>();
        replaceDefaultTie.Should().Throw<DomainException>();
        replaceRoundTie.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StructureLocked);
        replaceStandingOnCup.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StandingRulesInvariant);

        // Arrange — Championship (classifying): standing remains mutable after Start
        var league = Stage.Create(
            CompetitionId.New(),
            new StageName("League"),
            StageRegulation.MaterializeFrom(SampleRegulations.Standard(), isClassifyingPhase: true),
            _clock);
        league.AddMatchday(1, _clock);
        league.Prepare(_clock);
        league.Start(_clock);
        var standing = new StandingRules(
            new PointsPolicy(2, 1, 0),
            [RankingCriterion.Points, RankingCriterion.Wins]);

        league.ReplaceStandingRules(standing, _clock);

        league.Regulation.StandingRules.Should().Be(standing);
        league.Status.Should().Be(StageStatus.Running);
    }

    [Fact]
    public void Changing_stage_default_tie_format_does_not_change_existing_rounds()
    {
        // Arrange
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            new StageRegulation(
                SampleRegulations.Standard().MatchRules,
                standingRules: null,
                new TieFormat(2, true)),
            _clock);
        var round = stage.AddRound("QF", _clock);

        // Act
        stage.ReplaceDefaultTieFormat(new TieFormat(1, false), _clock);

        // Assert
        stage.Regulation.TieFormat!.NumberOfLegs.Should().Be(1);
        round.TieFormat!.NumberOfLegs.Should().Be(2);
    }

    private Stage CreateRunningCupStage()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            new StageRegulation(
                SampleRegulations.Standard().MatchRules,
                standingRules: null,
                new TieFormat(2, true),
                new DrawRules(DrawMode.Random)),
            _clock);
        stage.AddRound("QF", _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        return stage;
    }
}
