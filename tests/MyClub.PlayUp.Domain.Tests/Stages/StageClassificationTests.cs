// -----------------------------------------------------------------------
// <copyright file="StageClassificationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Stages;

public sealed class StageClassificationTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void MaterializeFrom_classifying_seeds_competition_standing_defaults()
    {
        var competitionRegulation = SampleRegulations.Standard();

        var stageRegulation = StageRegulation.MaterializeFrom(competitionRegulation, isClassifyingPhase: true);

        stageRegulation.StandingRules.Should().Be(competitionRegulation.StandingRules);
        ReferenceEquals(stageRegulation.StandingRules, competitionRegulation.StandingRules).Should().BeFalse();
    }

    [Fact]
    public void MaterializeFrom_non_classifying_leaves_standing_absent()
    {
        var competitionRegulation = SampleRegulations.Standard();

        var stageRegulation = StageRegulation.MaterializeFrom(competitionRegulation, isClassifyingPhase: false);

        stageRegulation.StandingRules.Should().BeNull();
        stageRegulation.MatchRules.Should().Be(competitionRegulation.MatchRules);
    }

    [Fact]
    public void Cup_topology_is_non_classifying_and_rejects_standing_presence()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Coupe"),
            StageRegulation.MaterializeFrom(SampleRegulations.Standard(), isClassifyingPhase: false),
            _clock);
        stage.AddRound("1/8", _clock);

        StageClassification.IsNonClassifyingPhase(stage).Should().BeTrue();
        StageClassification.IsClassifyingPhase(stage).Should().BeFalse();
        stage.Regulation.StandingRules.Should().BeNull();

        var withStanding = StageRegulation.MaterializeFrom(SampleRegulations.Standard(), isClassifyingPhase: true);
        var act = () => stage.ReplaceRegulation(withStanding, _clock);
        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StandingRulesInvariant);
    }

    [Fact]
    public void Championship_topology_requires_standing_and_seeds_from_defaults()
    {
        var defaults = SampleRegulations.Standard();
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Championnat"),
            StageRegulation.MaterializeFrom(defaults, isClassifyingPhase: false),
            _clock);

        stage.Regulation.StandingRules.Should().BeNull();
        stage.SeedStandingRules(defaults.StandingRules, _clock);
        stage.AddMatchday(1, _clock);

        StageClassification.IsClassifyingPhase(stage).Should().BeTrue();
        stage.Regulation.StandingRules.Should().Be(defaults.StandingRules);
    }

    [Fact]
    public void ReplaceStandingRules_rejects_non_classifying_stage()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Coupe"),
            StageRegulation.MaterializeFrom(SampleRegulations.Standard(), isClassifyingPhase: false),
            _clock);
        stage.AddRound("Finale", _clock);

        var act = () => stage.ReplaceStandingRules(SampleRegulations.Standard().StandingRules, _clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(StageErrorCodes.StandingRulesInvariant);
    }

    [Fact]
    public void Groups_without_qualification_still_classify()
    {
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Poules"),
            StageRegulation.MaterializeFrom(SampleRegulations.Standard(), isClassifyingPhase: true),
            _clock);
        stage.AddGroup("A", _clock);
        stage.AddMatchday(1, _clock);

        StageClassification.IsClassifyingPhase(stage).Should().BeTrue();
        stage.Regulation.QualificationRules.Should().BeNull();
        stage.Regulation.StandingRules.Should().NotBeNull();
    }
}
