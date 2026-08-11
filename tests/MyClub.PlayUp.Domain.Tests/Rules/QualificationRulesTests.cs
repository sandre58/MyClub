// -----------------------------------------------------------------------
// <copyright file="QualificationRulesTests.cs" company="Stéphane ANDRE">
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

public sealed class QualificationRulesTests
{
    [Fact]
    public void Constructor_accepts_group_top_to_main_bracket()
    {
        // Arrange
        var groupId = GroupId.New();
        var destinationStageId = StageId.New();
        var path = new QualificationPath(
            order: 1,
            QualificationSource.FromGroup(groupId),
            new QualificationSelection(SelectionMode.Top, 2),
            new QualificationDestination(destinationStageId, "QuarterFinal1"));

        // Act
        var rules = new QualificationRules([path]);

        // Assert
        rules.Paths.Should().ContainSingle();
        rules.Paths[0].Source.GroupId.Should().Be(groupId);
        rules.Paths[0].Selection.Mode.Should().Be(SelectionMode.Top);
        rules.Paths[0].Selection.Value.Should().Be(2);
        rules.Paths[0].Destination.SlotKey.Should().Be("QuarterFinal1");
    }

    [Fact]
    public void Constructor_accepts_main_bracket_and_consolante_paths()
    {
        // Arrange
        var groupId = GroupId.New();
        var mainStage = StageId.New();
        var consolanteStage = StageId.New();
        var paths = new[]
        {
            new QualificationPath(
                1,
                QualificationSource.FromGroup(groupId),
                new QualificationSelection(SelectionMode.Top, 2),
                new QualificationDestination(mainStage, "Main1")),
            new QualificationPath(
                2,
                QualificationSource.FromGroup(groupId),
                new QualificationSelection(SelectionMode.Bottom, 2),
                new QualificationDestination(consolanteStage, "Consolante1"))
        };

        // Act
        var rules = new QualificationRules(paths);

        // Assert
        rules.Paths.Should().HaveCount(2);
        rules.Paths[0].Order.Should().Be(1);
        rules.Paths[1].Destination.SlotKey.Should().Be("Consolante1");
    }

    [Fact]
    public void Constructor_accepts_selection_mode_best_as_top_alias_not_best_third()
    {
        // Arrange — Best is a Top alias on the supplied standing; not AcrossGroups / Best Third.
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Best, 4),
            new QualificationDestination(StageId.New(), "RoundOf16Slot"));

        // Act
        var rules = new QualificationRules([path]);

        // Assert
        rules.Paths[0].Source.Scope.Should().Be(RankingScope.Overall);
        rules.Paths[0].Selection.Mode.Should().Be(SelectionMode.Best);
        rules.Paths[0].Selection.Value.Should().Be(4);
        rules.Paths[0].Source.AcrossGroupsPosition.Should().BeNull();
    }

    [Fact]
    public void AcrossGroups_paths_accept_position_selection()
    {
        // Arrange — CDM-like: AcrossGroups(3) + Position 1..4 (not Overall + Best).
        var destination = StageId.New();
        var paths = Enumerable.Range(1, 4)
            .Select(i => new QualificationPath(
                i,
                QualificationSource.AcrossGroups(3),
                new QualificationSelection(SelectionMode.Position, i),
                new QualificationDestination(destination, $"R16-{i}")))
            .ToArray();

        // Act
        var rules = new QualificationRules(paths);

        // Assert
        rules.Paths.Should().HaveCount(4);
        rules.Paths.Should().OnlyContain(p =>
            p.Source.Scope == RankingScope.AcrossGroups
            && p.Source.AcrossGroupsPosition == 3
            && p.Selection.Mode == SelectionMode.Position);
        rules.Paths.Select(p => p.Selection.Value).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void Path_Copy_preserves_across_groups_position()
    {
        var original = new QualificationPath(
            1,
            QualificationSource.AcrossGroups(3),
            new QualificationSelection(SelectionMode.Position, 1),
            new QualificationDestination(StageId.New(), "Slot"));

        var copy = original.Copy();

        copy.Should().Be(original);
        copy.Source.AcrossGroupsPosition.Should().Be(3);
        ReferenceEquals(copy.Source, original.Source).Should().BeFalse();
    }

    [Fact]
    public void Equality_is_structural()
    {
        // Arrange
        var groupId = GroupId.New();
        var stageId = StageId.New();

        // Act
        var left = new QualificationRules([createPath()]);
        var right = new QualificationRules([createPath()]);

        // Assert
        left.Should().Be(right);
        left.GetHashCode().Should().Be(right.GetHashCode());
        return;

        QualificationPath createPath() => new(
            1,
            QualificationSource.FromGroup(groupId),
            new QualificationSelection(SelectionMode.Position, 1),
            new QualificationDestination(stageId, "Semi1"));
    }

    [Fact]
    public void Constructor_rejects_duplicate_orders()
    {
        // Arrange
        var stageId = StageId.New();
        var paths = new[]
        {
            new QualificationPath(1, QualificationSource.Overall(), new QualificationSelection(SelectionMode.Top, 1), new QualificationDestination(stageId, "A")),
            new QualificationPath(1, QualificationSource.Overall(), new QualificationSelection(SelectionMode.Top, 2), new QualificationDestination(stageId, "B"))
        };

        // Act
        var act = () => new QualificationRules(paths);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_empty_paths()
    {
        // Arrange & Act
        var act = () => new QualificationRules([]);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Selection_rejects_non_positive_value()
    {
        // Arrange & Act
        var act = () => new QualificationSelection(SelectionMode.Top, 0);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Destination_rejects_empty_slot_key()
    {
        // Arrange & Act
        var act = () => new QualificationDestination(StageId.New(), "   ");

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Path_rejects_non_positive_order()
    {
        // Arrange & Act
        var act = () => new QualificationPath(
            0,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Top, 1),
            new QualificationDestination(StageId.New(), "Slot"));

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Source_rejects_group_scope_without_group_id()
    {
        // Arrange & Act
        var act = () => new QualificationSource(RankingScope.Group);

        // Assert
        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Path_rejects_null_destination()
    {
        // Arrange & Act
        var act = () => new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Top, 1),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("destination");
    }

    [Fact]
    public void StageRegulation_can_carry_optional_qualification_rules()
    {
        // Arrange
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 6, 12, 0, 0, TimeSpan.Zero));
        var qualification = new QualificationRules(
        [
            new QualificationPath(
                1,
                QualificationSource.Overall(),
                new QualificationSelection(SelectionMode.Top, 4),
                new QualificationDestination(StageId.New(), "QF1"))
        ]);
        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            SampleRegulations.Standard().StandingRules,
            qualificationRules: qualification);

        // Act
        var stage = StageAggregate.Create(CompetitionId.New(), new StageName("Poules"), regulation, clock);

        // Assert
        stage.Regulation.QualificationRules.Should().Be(qualification);
        ReferenceEquals(stage.Regulation.QualificationRules, qualification).Should().BeFalse();
    }

    [Fact]
    public void Copy_creates_independent_path_instances()
    {
        // Arrange
        var original = new QualificationRules(
        [
            new QualificationPath(
                1,
                QualificationSource.Overall(),
                new QualificationSelection(SelectionMode.Best, 4),
                new QualificationDestination(StageId.New(), "Slot1"))
        ]);

        // Act
        var copy = original.Copy();

        // Assert
        copy.Should().Be(original);
        ReferenceEquals(copy.Paths[0], original.Paths[0]).Should().BeFalse();
        ReferenceEquals(copy.Paths[0].Destination, original.Paths[0].Destination).Should().BeFalse();
    }
}
