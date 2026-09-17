// -----------------------------------------------------------------------
// <copyright file="StageRegulationJsonConverterTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Infrastructure.Persistence.Converters;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence.Converters;

public sealed class StageRegulationJsonConverterTests
{
    private readonly StageRegulationJsonConverter _converter = new();
    private readonly TieFormatJsonConverter _tieFormatConverter = new();

    [Fact]
    public void StageRegulation_convert_round_trips()
    {
        var regulation = StageRegulation.MaterializeFrom(SampleRegulations.WithExtraTimeAndShootout())
            .WithTieFormat(new TieFormat(2, true, new AwayGoalsRule()));

        var json = _converter.ConvertToProvider(regulation);
        var restored = _converter.ConvertFromProvider(json);

        restored.Should().Be(regulation);
    }

    [Fact]
    public void StageRegulation_convert_round_trips_draw_qualification_progression_and_placement_award_families()
    {
        var groupId = GroupId.New();
        var destinationStageId = StageId.New();
        var fixtureId = FixtureId.New();
        var progressionStageId = StageId.New();

        var regulation = StageRegulation.MaterializeFrom(SampleRegulations.Standard())
            .WithTieFormat(new TieFormat(2, true, new AwayGoalsRule()))
            .WithDrawRules(
                new DrawRules(
                    DrawMode.Random,
                    new SeedingRules(4),
                    new PotRules(2),
                    [
                        new DrawConstraint(DrawConstraintType.SameTeamAvoidance),
                        new DrawConstraint(DrawConstraintType.SameAssociationAvoidance, ConstraintEnforcement.Required),
                        DrawConstraint.MaxSameAssociationPerGroup(2)
                    ]))
            .WithQualificationRules(
                new QualificationRules(
                [
                    new QualificationPath(
                        order: 1,
                        QualificationSource.FromGroup(groupId),
                        new QualificationSelection(SelectionMode.Position, 1),
                        QualificationDestination.ForPopulation(destinationStageId),
                        QualificationCondition.PointsAtLeast(6))
                ]))
            .WithProgressionRules(
                new ProgressionRules(
                [
                    new ProgressionPath(
                        fixtureId,
                        ProgressionOutcome.Winner,
                        ProgressionDestination.ForPopulation(progressionStageId)),
                    new ProgressionPath(
                        fixtureId,
                        ProgressionOutcome.Loser,
                        ProgressionDestination.ForPopulation(progressionStageId))
                ]))
            .WithPlacementAwardRules(
                new PlacementAwardRules(
                [
                    new PlacementAwardPath(fixtureId, ProgressionOutcome.Winner, rank: 3),
                    new PlacementAwardPath(fixtureId, ProgressionOutcome.Loser, rank: 4)
                ]));

        var json = _converter.ConvertToProvider(regulation).Should().BeOfType<string>().Subject;
        var restored = _converter.ConvertFromProvider(json);

        restored.Should().Be(regulation);
        json.Should().Contain("\"ConstraintType\"");
        json.Should().Contain("\"MaxPerGroup\":2");
        json.Should().Contain("\"MinimumPoints\":6");
        json.Should().Contain("\"DrawRules\"");
        json.Should().Contain("\"QualificationRules\"");
        json.Should().Contain("\"ProgressionRules\"");
        json.Should().Contain("\"PlacementAwardRules\"");
    }

    [Fact]
    public void TieFormat_convert_round_trips_and_null()
    {
        var tieFormat = new TieFormat(1, false, extraTimeRule: new ExtraTimeRule(), penaltyShootoutRule: new PenaltyShootoutRule());

        _tieFormatConverter.ConvertFromProvider(_tieFormatConverter.ConvertToProvider(tieFormat)).Should().Be(tieFormat);
        _tieFormatConverter.ConvertFromProvider(_tieFormatConverter.ConvertToProvider(null)).Should().BeNull();
    }

    [Fact]
    public void TieFormat_convert_uses_pascal_case_and_numeric_enums_for_markers()
    {
        var tieFormat = new TieFormat(2, true, new AwayGoalsRule(), new ExtraTimeRule(), new PenaltyShootoutRule());

        var json = _tieFormatConverter.ConvertToProvider(tieFormat).Should().BeOfType<string>().Subject;

        json.Should().Contain("\"NumberOfLegs\":2");
        json.Should().Contain("\"AggregateScoring\":true");
        json.Should().Contain("\"AwayGoalsRule\"");
        json.Should().Contain("\"ExtraTimeRule\"");
        json.Should().Contain("\"PenaltyShootoutRule\"");
        json.Should().NotContain("\"numberOfLegs\"");
    }
}
