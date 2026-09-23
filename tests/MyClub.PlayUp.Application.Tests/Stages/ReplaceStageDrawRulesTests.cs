// -----------------------------------------------------------------------
// <copyright file="ReplaceStageDrawRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class ReplaceStageDrawRulesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 23, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Replace_without_constraints_preserves_existing_constraints()
    {
        var competition = CreateCompetition.Execute("PreserveConstraints", _clock);
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock).Stage;

        var constrained = new DrawRules(
            DrawMode.Random,
            seedingRules: new SeedingRules(2),
            potRules: new PotRules(4),
            constraints:
            [
                new DrawConstraint(DrawConstraintType.SameAssociationAvoidance),
                DrawConstraint.MaxSameAssociationPerGroup(2),
            ]);
        ReplaceStageDrawRules.Execute(stage, constrained, _clock);

        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, new SeedingRules(1), new PotRules(2)),
            _clock);

        var rules = stage.Regulation.DrawRules;
        rules.Should().NotBeNull();
        rules!.SeedingRules!.NumberOfSeeds.Should().Be(1);
        rules.PotRules!.NumberOfPots.Should().Be(2);
        rules.Constraints.Should().HaveCount(2);
        rules.Constraints[0].ConstraintType.Should().Be(DrawConstraintType.SameAssociationAvoidance);
        rules.Constraints[1].ConstraintType.Should().Be(DrawConstraintType.MaxSameAssociationPerGroup);
        rules.Constraints[1].MaxPerGroup.Should().Be(2);
    }

    [Fact]
    public void Replace_with_explicit_constraints_replaces_constraint_list()
    {
        var competition = CreateCompetition.Execute("ExplicitConstraints", _clock);
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;

        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(
                DrawMode.Random,
                constraints: [new DrawConstraint(DrawConstraintType.SameTeamAvoidance)]),
            _clock);

        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(
                DrawMode.Random,
                constraints: [new DrawConstraint(DrawConstraintType.SameGroupAvoidance)]),
            _clock);

        stage.Regulation.DrawRules!.Constraints.Should().HaveCount(1);
        stage.Regulation.DrawRules.Constraints[0].ConstraintType
            .Should().Be(DrawConstraintType.SameGroupAvoidance);
    }

    [Fact]
    public void Replace_without_seeds_preserves_existing_seeding_rules()
    {
        var competition = CreateCompetition.Execute("PreserveSeeds", _clock);
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;

        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, seedingRules: new SeedingRules(4)),
            _clock);

        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);

        var rules = stage.Regulation.DrawRules;
        rules.Should().NotBeNull();
        rules!.SeedingRules!.NumberOfSeeds.Should().Be(4);
        rules.PotRules!.NumberOfPots.Should().Be(2);
    }

    [Fact]
    public void Clear_removes_draw_rules_including_constraints()
    {
        var competition = CreateCompetition.Execute("ClearConstraints", _clock);
        var stage = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Cup(4),
            _clock).Stage;

        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(
                DrawMode.Random,
                constraints: [new DrawConstraint(DrawConstraintType.SameTeamAvoidance)]),
            _clock);

        ReplaceStageDrawRules.Execute(stage, null, _clock);

        stage.Regulation.DrawRules.Should().BeNull();
    }
}
