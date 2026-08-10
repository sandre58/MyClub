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
    public void SelectEntries_best_is_not_supported()
    {
        var standing = BuildStanding();

        var act = () => QualificationApplier.SelectEntries(
            standing,
            new QualificationSelection(SelectionMode.Best, 2));

        act.Should().Throw<DomainException>().Which.Code.Should().Be(QualificationErrorCodes.SelectionNotSupported);
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
