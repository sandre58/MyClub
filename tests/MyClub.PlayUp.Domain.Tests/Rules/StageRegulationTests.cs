// -----------------------------------------------------------------------
// <copyright file="StageRegulationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class StageRegulationTests
{
    [Fact]
    public void MaterializeFrom_copies_match_and_standing_values_when_classifying()
    {
        // Arrange
        var competitionRegulation = SampleRegulations.Standard();

        // Act
        var stageRegulation = StageRegulation.MaterializeFrom(competitionRegulation, isClassifyingPhase: true);

        // Assert
        stageRegulation.MatchRules.Should().Be(competitionRegulation.MatchRules);
        stageRegulation.StandingRules.Should().Be(competitionRegulation.StandingRules);
    }

    [Fact]
    public void MaterializeFrom_uses_independent_nested_instances()
    {
        // Arrange
        var competitionRegulation = SampleRegulations.Standard();

        // Act
        var stageRegulation = StageRegulation.MaterializeFrom(competitionRegulation, isClassifyingPhase: true);

        // Assert
        ReferenceEquals(stageRegulation.MatchRules, competitionRegulation.MatchRules).Should().BeFalse();
        ReferenceEquals(stageRegulation.StandingRules, competitionRegulation.StandingRules).Should().BeFalse();
        ReferenceEquals(stageRegulation.MatchRules.Duration, competitionRegulation.MatchRules.Duration)
            .Should().BeFalse();
    }

    [Fact]
    public void MaterializeFrom_non_classifying_omits_standing()
    {
        var competitionRegulation = SampleRegulations.Standard();

        var stageRegulation = StageRegulation.MaterializeFrom(competitionRegulation, isClassifyingPhase: false);

        stageRegulation.StandingRules.Should().BeNull();
    }
}
