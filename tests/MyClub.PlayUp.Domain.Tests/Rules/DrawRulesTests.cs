// -----------------------------------------------------------------------
// <copyright file="DrawRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;
using StageAggregate = MyClub.PlayUp.Domain.Stage.Stage;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class DrawRulesTests
{
    [Fact]
    public void Constructor_accepts_random_mode()
    {
        // Arrange & Act
        var rules = new DrawRules(DrawMode.Random);

        // Assert
        rules.Mode.Should().Be(DrawMode.Random);
        rules.SeedingRules.Should().BeNull();
        rules.PotRules.Should().BeNull();
        rules.Constraints.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_accepts_random_with_seeds_pots_and_constraints()
    {
        // Arrange
        var constraints = new[]
        {
            new DrawConstraint(DrawConstraintType.SameGroupAvoidance),
            new DrawConstraint(DrawConstraintType.SameAssociationAvoidance, ConstraintEnforcement.Required)
        };

        // Act
        var rules = new DrawRules(
            DrawMode.Random,
            new SeedingRules(4),
            new PotRules(4),
            constraints);

        // Assert
        rules.Mode.Should().Be(DrawMode.Random);
        rules.SeedingRules!.NumberOfSeeds.Should().Be(4);
        rules.PotRules!.NumberOfPots.Should().Be(4);
        rules.Constraints.Should().Equal(constraints);
        rules.Constraints[0].Enforcement.Should().Be(ConstraintEnforcement.Preferred);
        rules.Constraints[1].Enforcement.Should().Be(ConstraintEnforcement.Required);
    }

    [Fact]
    public void DrawMode_has_only_Random_in_V1() =>
        Enum.GetNames<DrawMode>().Should().BeEquivalentTo("Random");

    [Fact]
    public void Equality_is_structural()
    {
        // Arrange & Act
        var left = new DrawRules(
            DrawMode.Random,
            new SeedingRules(2),
            constraints: [new DrawConstraint(DrawConstraintType.SameTeamAvoidance)]);
        var right = new DrawRules(
            DrawMode.Random,
            new SeedingRules(2),
            constraints: [new DrawConstraint(DrawConstraintType.SameTeamAvoidance)]);

        // Assert
        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
    }

    [Fact]
    public void SeedingRules_rejects_negative_seeds()
    {
        // Arrange & Act
        var act = () => new SeedingRules(-1);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.DrawRulesInvalid);
    }

    [Fact]
    public void PotRules_rejects_fewer_than_two_pots()
    {
        // Arrange & Act
        var act = () => new PotRules(1);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.DrawRulesInvalid);
    }

    [Fact]
    public void DrawConstraint_rejects_unknown_type()
    {
        // Arrange & Act
        var act = () => new DrawConstraint((DrawConstraintType)999);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.DrawRulesInvalid);
    }

    [Fact]
    public void DrawRules_rejects_unknown_mode()
    {
        // Arrange & Act
        var act = () => new DrawRules((DrawMode)999);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.DrawRulesInvalid);
    }

    [Fact]
    public void Copy_creates_independent_instances()
    {
        // Arrange
        var original = new DrawRules(
            DrawMode.Random,
            new SeedingRules(8),
            new PotRules(4),
            [new DrawConstraint(DrawConstraintType.SameGroupAvoidance, ConstraintEnforcement.Required)]);

        // Act
        var copy = original.Copy();

        // Assert
        copy.Should().Be(original);
        ReferenceEquals(copy.SeedingRules, original.SeedingRules).Should().BeFalse();
        ReferenceEquals(copy.PotRules, original.PotRules).Should().BeFalse();
        ReferenceEquals(copy.Constraints[0], original.Constraints[0]).Should().BeFalse();
    }

    [Fact]
    public void StageRegulation_can_carry_optional_draw_rules()
    {
        // Arrange
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));
        var drawRules = new DrawRules(DrawMode.Random, new SeedingRules(4));
        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            SampleRegulations.Standard().StandingRules,
            drawRules: drawRules);

        // Act
        var stage = StageAggregate.Create(CompetitionId.New(), new StageName("Cup"), regulation, clock);

        // Assert
        stage.Regulation.DrawRules.Should().Be(drawRules);
        ReferenceEquals(stage.Regulation.DrawRules, drawRules).Should().BeFalse();
        regulation.DrawRules.Should().Be(drawRules);
    }

    [Fact]
    public void WithDrawRules_replaces_draw_family_only()
    {
        // Arrange
        var baseRegulation = StageRegulation.MaterializeFrom(SampleRegulations.Standard());
        var drawRules = new DrawRules(DrawMode.Random, potRules: new PotRules(2));

        // Act
        var updated = baseRegulation.WithDrawRules(drawRules);

        // Assert
        updated.DrawRules.Should().Be(drawRules);
        updated.MatchRules.Should().Be(baseRegulation.MatchRules);
        updated.StandingRules.Should().Be(baseRegulation.StandingRules);
        updated.TieFormat.Should().BeNull();
    }
}
