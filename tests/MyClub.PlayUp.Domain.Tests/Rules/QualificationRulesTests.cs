// -----------------------------------------------------------------------
// <copyright file="QualificationRulesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class QualificationRulesTests
{
    [Fact]
    public void Constructor_accepts_group_top_to_population()
    {
        // Arrange
        var groupId = GroupId.New();
        var destinationStageId = StageId.New();
        var path = new QualificationPath(
            order: 1,
            QualificationSource.FromGroup(groupId),
            new QualificationSelection(SelectionMode.Top, 2),
            QualificationDestination.ForPopulation(destinationStageId));

        // Act
        var rules = new QualificationRules([path]);

        // Assert
        rules.Paths.Should().ContainSingle();
        rules.Paths[0].Source.GroupId.Should().Be(groupId);
        rules.Paths[0].Selection.Mode.Should().Be(SelectionMode.Top);
        rules.Paths[0].Selection.Value.Should().Be(2);
        rules.Paths[0].Destination.StageId.Should().Be(destinationStageId);
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
                QualificationDestination.ForPopulation(mainStage)),
            new QualificationPath(
                2,
                QualificationSource.FromGroup(groupId),
                new QualificationSelection(SelectionMode.Bottom, 2),
                QualificationDestination.ForPopulation(consolanteStage))
        };

        // Act
        var rules = new QualificationRules(paths);

        // Assert
        rules.Paths.Should().HaveCount(2);
        rules.Paths[0].Order.Should().Be(1);
        rules.Paths[0].Destination.StageId.Should().Be(mainStage);
        rules.Paths[1].Destination.StageId.Should().Be(consolanteStage);
    }

    [Fact]
    public void Constructor_accepts_selection_mode_best_as_top_alias_not_best_third()
    {
        // Arrange — Best is a Top alias on the supplied standing; not AcrossGroups / Best Third.
        var destination = StageId.New();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Best, 4),
            QualificationDestination.ForPopulation(destination));

        // Act
        var rules = new QualificationRules([path]);

        // Assert
        rules.Paths[0].Source.Scope.Should().Be(RankingScope.Overall);
        rules.Paths[0].Selection.Mode.Should().Be(SelectionMode.Best);
        rules.Paths[0].Selection.Value.Should().Be(4);
        rules.Paths[0].Source.AcrossGroupsPosition.Should().BeNull();
        rules.Paths[0].Destination.StageId.Should().Be(destination);
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
                QualificationDestination.ForPopulation(destination)))
            .ToArray();

        // Act
        var rules = new QualificationRules(paths);

        // Assert
        rules.Paths.Should().HaveCount(4);
        rules.Paths.Should().OnlyContain(p =>
            p.Source.Scope == RankingScope.AcrossGroups
            && p.Source.AcrossGroupsPosition == 3
            && p.Selection.Mode == SelectionMode.Position
            && p.Destination.StageId.Equals(destination));
        rules.Paths.Select(p => p.Selection.Value).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void Path_Copy_preserves_across_groups_position()
    {
        var original = new QualificationPath(
            1,
            QualificationSource.AcrossGroups(3),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForPopulation(StageId.New()));

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
            QualificationDestination.ForPopulation(stageId));
    }

    [Fact]
    public void Constructor_rejects_duplicate_orders()
    {
        // Arrange
        var stageId = StageId.New();
        var paths = new[]
        {
            new QualificationPath(1, QualificationSource.Overall(), new QualificationSelection(SelectionMode.Top, 1), QualificationDestination.ForPopulation(stageId)),
            new QualificationPath(1, QualificationSource.Overall(), new QualificationSelection(SelectionMode.Top, 2), QualificationDestination.ForPopulation(stageId))
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
    public void Path_rejects_non_positive_order()
    {
        // Arrange & Act
        var act = () => new QualificationPath(
            0,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Top, 1),
            QualificationDestination.ForPopulation(StageId.New()));

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
                QualificationDestination.ForPopulation(StageId.New()))
        ]);
        var regulation = new StageRegulation(
            SampleRegulations.Standard().MatchRules,
            SampleRegulations.Standard().StandingRules,
            qualificationRules: qualification);

        // Act
        var stage = Stage.Create(CompetitionId.New(), new StageName("Poules"), regulation, clock);

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
                QualificationDestination.ForPopulation(StageId.New()))
        ]);

        // Act
        var copy = original.Copy();

        // Assert
        copy.Should().Be(original);
        ReferenceEquals(copy.Paths[0], original.Paths[0]).Should().BeFalse();
        ReferenceEquals(copy.Paths[0].Destination, original.Paths[0].Destination).Should().BeFalse();
    }

    [Fact]
    public void ReplaceQualificationRules_rejects_destination_targeting_source_stage()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Cup"),
            SampleRegulations.Standard(),
            clock);

        var act = () => stage.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(stage.Id))
            ]),
            clock);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void ReplaceQualificationRules_accepts_peer_stage_population_destination()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 12, 12, 0, 0, TimeSpan.Zero));
        var stage = Stage.Create(
            CompetitionId.New(),
            new StageName("Poules"),
            SampleRegulations.Standard(),
            clock);
        var peerStageId = StageId.New();

        stage.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForPopulation(peerStageId))
            ]),
            clock);

        stage.Regulation.QualificationRules!.Paths.Should().ContainSingle();
        stage.Regulation.QualificationRules.Paths[0].Destination.StageId.Should().Be(peerStageId);
    }
}
