// -----------------------------------------------------------------------
// <copyright file="QualificationApplierTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Qualification;

public sealed class QualificationApplierTests
{
    private readonly EntryId _a = EntryId.New();
    private readonly EntryId _b = EntryId.New();
    private readonly EntryId _c = EntryId.New();
    private readonly EntryId _d = EntryId.New();
    private readonly StageId _stageId = StageId.New();

    [Fact]
    public void SelectEntries_position_returns_single_entry()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Position, 2));

        selected.Should().Equal(_b);
    }

    [Fact]
    public void SelectEntries_top_returns_first_n()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Top, 2));

        selected.Should().Equal(_a, _b);
    }

    [Fact]
    public void SelectEntries_bottom_returns_last_n()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Bottom, 2));

        selected.Should().Equal(_c, _d);
    }

    [Fact]
    public void SelectEntries_range_returns_inclusive_slice()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Range, 2, 3));

        selected.Should().Equal(_b, _c);
    }

    [Fact]
    public void SelectEntries_best_1_returns_leader()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Top, 1));

        selected.Should().Equal(_a);
    }

    [Fact]
    public void SelectEntries_best_n_returns_first_n()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Top, 2));

        selected.Should().Equal(_a, _b);
    }

    [Fact]
    public void SelectEntries_worst_1_returns_last()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Bottom, 1));

        selected.Should().Equal(_d);
    }

    [Fact]
    public void SelectEntries_worst_n_returns_last_n()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Bottom, 2));

        selected.Should().Equal(_c, _d);
    }

    [Fact]
    public void Apply_best_1_returns_instruction()
    {
        var standing = BuildStanding();
        var path = PopulationPath(new QualificationSelection(SelectionMode.Top, 1));

        var instruction = QualificationApplier.Apply(path, standing);

        instruction!.StageId.Should().Be(_stageId);
        instruction.EntryId.Should().Be(_a);
    }

    [Fact]
    public void Apply_best_n_rejects_path_multi_entry()
    {
        var standing = BuildStanding();
        var path = PopulationPath(new QualificationSelection(SelectionMode.Top, 2));

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.PathMultiEntry);
    }

    [Fact]
    public void Apply_worst_1_returns_instruction()
    {
        var standing = BuildStanding();
        var path = PopulationPath(new QualificationSelection(SelectionMode.Bottom, 1));

        var instruction = QualificationApplier.Apply(path, standing);

        instruction!.StageId.Should().Be(_stageId);
        instruction.EntryId.Should().Be(_d);
    }

    [Fact]
    public void Apply_worst_n_rejects_path_multi_entry()
    {
        var standing = BuildStanding();
        var path = PopulationPath(new QualificationSelection(SelectionMode.Bottom, 2));

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.PathMultiEntry);
    }

    [Fact]
    public void Selection_rejects_best_zero()
    {
        var act = () => new QualificationSelection(SelectionMode.Top, 0);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Selection_rejects_worst_negative()
    {
        var act = () => new QualificationSelection(SelectionMode.Bottom, -1);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Apply_position_returns_qualification_instruction()
    {
        var standing = BuildStanding();
        var path = PopulationPath(new QualificationSelection(SelectionMode.Position, 1));

        var instruction = QualificationApplier.Apply(path, standing);

        instruction.Should().Be(new QualificationInstruction(_stageId, _a));
    }

    [Fact]
    public void Apply_range_from_equals_to_is_single_entry()
    {
        var standing = BuildStanding();
        var path = PopulationPath(new QualificationSelection(SelectionMode.Range, 3, 3));

        var instruction = QualificationApplier.Apply(path, standing);

        instruction!.StageId.Should().Be(_stageId);
        instruction.EntryId.Should().Be(_c);
    }

    [Fact]
    public void Apply_rejects_multi_entry_selection()
    {
        var standing = BuildStanding();
        var path = PopulationPath(new QualificationSelection(SelectionMode.Top, 2));

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.PathMultiEntry);
    }

    [Fact]
    public void Apply_rejects_unresolved_position()
    {
        var standing = BuildStanding();
        var path = PopulationPath(new QualificationSelection(SelectionMode.Position, 99));

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.SelectionUnresolved);
    }

    [Fact]
    public void Apply_condition_satisfied_returns_instruction()
    {
        var standing = ManualStanding([(_a, 1, 50), (_b, 2, 45), (_c, 3, 42)]);
        var path = ConditionalPositionPath(3, minimumPoints: 40);

        var instruction = QualificationApplier.Apply(path, standing);

        instruction.Should().NotBeNull();
        instruction.Should().Be(new QualificationInstruction(_stageId, _c));
    }

    [Fact]
    public void Apply_condition_at_threshold_returns_instruction()
    {
        var standing = ManualStanding([(_a, 1, 50), (_b, 2, 45), (_c, 3, 40)]);
        var path = ConditionalPositionPath(3, minimumPoints: 40);

        var instruction = QualificationApplier.Apply(path, standing);

        instruction.Should().NotBeNull();
        instruction.EntryId.Should().Be(_c);
        instruction.StageId.Should().Be(_stageId);
    }

    [Fact]
    public void Apply_condition_not_satisfied_returns_null_skip()
    {
        var standing = ManualStanding([(_a, 1, 50), (_b, 2, 45), (_c, 3, 39)]);
        var path = ConditionalPositionPath(3, minimumPoints: 40);

        var instruction = QualificationApplier.Apply(path, standing);

        instruction.Should().BeNull();
    }

    [Fact]
    public void Apply_condition_with_missing_position_is_unresolved_not_skip()
    {
        var standing = ManualStanding([(_a, 1, 50), (_b, 2, 45)]);
        var path = ConditionalPositionPath(3, minimumPoints: 40);

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.SelectionUnresolved);
    }

    [Theory]
    [InlineData(SelectionMode.Top)]
    [InlineData(SelectionMode.Bottom)]
    public void Path_rejects_condition_with_non_position_mode(SelectionMode mode)
    {
        var act = () => new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(mode, 1),
            QualificationDestination.ForPopulation(_stageId),
            QualificationCondition.PointsAtLeast(40));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Path_rejects_condition_with_range()
    {
        var act = () => new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Range, 1, 1),
            QualificationDestination.ForPopulation(_stageId),
            QualificationCondition.PointsAtLeast(40));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Path_Copy_preserves_condition()
    {
        var original = ConditionalPositionPath(3, 40);

        var copy = original.Copy();

        copy.Should().Be(original);
        copy.Condition!.MinimumPoints.Should().Be(40);
        ReferenceEquals(copy.Condition, original.Condition).Should().BeFalse();
    }

    private static Standing ManualStanding(IReadOnlyList<(EntryId EntryId, int Position, int Points)> rows) =>
        new(
        [
            ..rows.Select(r => new StandingRow(
                r.EntryId,
                r.Position,
                played: 0,
                wins: 0,
                draws: 0,
                losses: 0,
                goalsFor: 0,
                goalsAgainst: 0,
                points: r.Points))
        ]);

    private QualificationPath PopulationPath(QualificationSelection selection) =>
        new(
            1,
            QualificationSource.Overall(),
            selection,
            QualificationDestination.ForPopulation(_stageId));

    private QualificationPath ConditionalPositionPath(int position, int minimumPoints) =>
        new(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, position),
            QualificationDestination.ForPopulation(_stageId),
            QualificationCondition.PointsAtLeast(minimumPoints));

    private Standing BuildStanding()
    {
        var rules = new StandingRules(
            new PointsPolicy(3, 1, 0),
            [RankingCriterion.Points, RankingCriterion.GoalDifference, RankingCriterion.GoalsFor]);

        var matches = new[]
        {
            new StandingMatch(_a, _b, 3, 0),
            new StandingMatch(_a, _c, 2, 0),
            new StandingMatch(_a, _d, 1, 0),
            new StandingMatch(_b, _c, 2, 0),
            new StandingMatch(_b, _d, 1, 0),
            new StandingMatch(_c, _d, 1, 0)
        };

        return StandingCalculator.Calculate([_a, _b, _c, _d], matches, rules);
    }
}
