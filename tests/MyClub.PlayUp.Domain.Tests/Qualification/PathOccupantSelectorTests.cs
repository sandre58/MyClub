// -----------------------------------------------------------------------
// <copyright file="PathOccupantSelectorTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;
using MyClub.PlayUp.Domain.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Qualification;

public sealed class PathOccupantSelectorTests
{
    private readonly EntryId _a = EntryId.New();
    private readonly EntryId _b = EntryId.New();
    private readonly EntryId _c = EntryId.New();

    [Fact]
    public void SelectQualification_matches_QualificationApplier_for_resolved_paths()
    {
        var standing = ManualStanding([(_a, 1, 9), (_b, 2, 6), (_c, 3, 3)]);
        var path = Path(SelectionMode.Position, 1);

        var selector = PathOccupantSelector.SelectQualification(path, standing);
        var applier = QualificationApplier.Apply(path, standing);

        selector.Status.Should().Be(PathOccupantSelectionStatus.Resolved);
        selector.EntryId.Should().Be(_a);
        applier!.EntryId.Should().Be(_a);
    }

    [Fact]
    public void SelectQualification_is_indeterminate_when_selection_is_ambiguous()
    {
        var standing = ManualStanding([(_a, 1, 6), (_b, 1, 6)]);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Top, 2),
            QualificationDestination.ForPopulation(StageId.New()));

        PathOccupantSelector.SelectQualification(path, standing).Status
            .Should()
            .Be(PathOccupantSelectionStatus.Indeterminate);
    }

    [Fact]
    public void SelectQualification_is_stable_across_repeated_runs_on_tied_standing()
    {
        var standing = ManualStanding([(_a, 1, 6), (_b, 2, 6), (_c, 3, 6)]);
        var path = Path(SelectionMode.Position, 2);

        var first = PathOccupantSelector.SelectQualification(path, standing);
        for (var i = 0; i < 20; i++)
        {
            PathOccupantSelector.SelectQualification(path, standing).Should().BeEquivalentTo(first);
        }
    }

    [Fact]
    public void SelectQualification_skips_when_condition_not_satisfied()
    {
        var standing = ManualStanding([(_a, 1, 2), (_b, 2, 8)]);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForPopulation(StageId.New()),
            QualificationCondition.PointsAtLeast(5));

        PathOccupantSelector.SelectQualification(path, standing).Status
            .Should()
            .Be(PathOccupantSelectionStatus.Skipped);
    }

    [Fact]
    public void SelectQualification_is_indeterminate_when_condition_row_missing()
    {
        var standing = new Standing([]);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForPopulation(StageId.New()),
            QualificationCondition.PointsAtLeast(1));

        PathOccupantSelector.SelectQualification(path, standing).Status
            .Should()
            .Be(PathOccupantSelectionStatus.Indeterminate);
    }

    [Fact]
    public void SelectProgression_resolves_winner_and_loser_from_fixture_outcome()
    {
        var winnerPath = ProgressionPath(
            ProgressionOutcome.Winner,
            StageId.New(),
            "P1");
        var loserPath = ProgressionPath(
            ProgressionOutcome.Loser,
            StageId.New(),
            "P1");
        var outcome = new FixtureOutcome(_a, _b);

        PathOccupantSelector.SelectProgression(winnerPath, outcome).EntryId.Should().Be(_a);
        PathOccupantSelector.SelectProgression(loserPath, outcome).EntryId.Should().Be(_b);
    }

    [Fact]
    public void SelectQualification_resolves_forfeit_winner_from_standing_order()
    {
        var winner = EntryId.New();
        var loser = EntryId.New();
        var standing = ManualStanding([(winner, 1, 3), (loser, 2, 0)]);
        var path = Path(SelectionMode.Position, 1);

        PathOccupantSelector.SelectQualification(path, standing).EntryId.Should().Be(winner);
    }

    [Fact]
    public void SelectQualification_is_indeterminate_when_top_selection_spans_withdrawn_gap()
    {
        var active = EntryId.New();
        var withdrawn = EntryId.New();
        var standing = ManualStanding([(active, 1, 9), (withdrawn, 2, 0)]);
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Top, 2),
            QualificationDestination.ForPopulation(StageId.New()));

        PathOccupantSelector.SelectQualification(path, standing).Status
            .Should()
            .Be(PathOccupantSelectionStatus.Indeterminate);
    }

    private static QualificationPath Path(SelectionMode mode, int value) =>
        new(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(mode, value),
            QualificationDestination.ForPopulation(StageId.New()));

    private static ProgressionPath ProgressionPath(
        ProgressionOutcome outcome,
        StageId destinationStageId,
        string pairKey) =>
        new(
            pairKey,
            outcome,
            ProgressionDestination.ForPopulation(destinationStageId));

    private Standing ManualStanding(IReadOnlyList<(EntryId EntryId, int Position, int Points)> rows) =>
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
}
