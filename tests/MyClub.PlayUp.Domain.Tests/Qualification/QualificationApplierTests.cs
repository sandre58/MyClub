// -----------------------------------------------------------------------
// <copyright file="QualificationApplierTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standing;
using Xunit;
using StandingView = MyClub.PlayUp.Domain.Standing.Standing;

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
            new QualificationSelection(SelectionMode.Best, 1));

        selected.Should().Equal(_a);
    }

    [Fact]
    public void SelectEntries_best_n_returns_first_n()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Best, 2));

        selected.Should().Equal(_a, _b);
    }

    [Fact]
    public void SelectEntries_worst_1_returns_last()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Worst, 1));

        selected.Should().Equal(_d);
    }

    [Fact]
    public void SelectEntries_worst_n_returns_last_n()
    {
        var standing = BuildStanding();

        var selected = QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Worst, 2));

        selected.Should().Equal(_c, _d);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SelectEntries_best_equals_top_for_n(int n)
    {
        var standing = BuildStanding();

        var best = QualificationApplier.SelectEntries(standing, new QualificationSelection(SelectionMode.Best, n));
        var top = QualificationApplier.SelectEntries(standing, new QualificationSelection(SelectionMode.Top, n));

        best.Should().Equal(top);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SelectEntries_worst_equals_bottom_for_n(int n)
    {
        var standing = BuildStanding();

        var worst = QualificationApplier.SelectEntries(standing, new QualificationSelection(SelectionMode.Worst, n));
        var bottom = QualificationApplier.SelectEntries(standing, new QualificationSelection(SelectionMode.Bottom, n));

        worst.Should().Equal(bottom);
    }

    [Fact]
    public void Apply_best_1_returns_instruction()
    {
        var standing = BuildStanding();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Best, 1),
            new QualificationDestination(_stageId, "Champ"));

        var instruction = QualificationApplier.Apply(path, standing);

        instruction.EntryId.Should().Be(_a);
    }

    [Fact]
    public void Apply_best_n_rejects_path_multi_entry()
    {
        var standing = BuildStanding();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Best, 2),
            new QualificationDestination(_stageId, "KO"));

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.PathMultiEntry);
    }

    [Fact]
    public void Apply_worst_1_returns_instruction()
    {
        var standing = BuildStanding();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Worst, 1),
            new QualificationDestination(_stageId, "Relegated"));

        var instruction = QualificationApplier.Apply(path, standing);

        instruction.EntryId.Should().Be(_d);
    }

    [Fact]
    public void Apply_worst_n_rejects_path_multi_entry()
    {
        var standing = BuildStanding();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Worst, 2),
            new QualificationDestination(_stageId, "KO"));

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.PathMultiEntry);
    }

    [Fact]
    public void Selection_rejects_best_zero()
    {
        var act = () => new QualificationSelection(SelectionMode.Best, 0);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Selection_rejects_worst_negative()
    {
        var act = () => new QualificationSelection(SelectionMode.Worst, -1);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Apply_position_returns_instruction()
    {
        var standing = BuildStanding();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            new QualificationDestination(_stageId, "Champ"));

        var instruction = QualificationApplier.Apply(path, standing);

        instruction.StageId.Should().Be(_stageId);
        instruction.SlotKey.Should().Be("Champ");
        instruction.EntryId.Should().Be(_a);
    }

    [Fact]
    public void Apply_range_from_equals_to_is_single_entry()
    {
        var standing = BuildStanding();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Range, 3, 3),
            new QualificationDestination(_stageId, "P3"));

        var instruction = QualificationApplier.Apply(path, standing);

        instruction.EntryId.Should().Be(_c);
    }

    [Fact]
    public void Apply_rejects_multi_entry_selection()
    {
        var standing = BuildStanding();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Top, 2),
            new QualificationDestination(_stageId, "KO"));

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.PathMultiEntry);
    }

    [Fact]
    public void Apply_rejects_unresolved_position()
    {
        var standing = BuildStanding();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 99),
            new QualificationDestination(_stageId, "X"));

        var act = () => QualificationApplier.Apply(path, standing);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.SelectionUnresolved);
    }

    private StandingView BuildStanding()
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
