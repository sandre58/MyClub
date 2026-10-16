// -----------------------------------------------------------------------
// <copyright file="ProgressionRulesPathDiffTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class ProgressionRulesPathDiffTests
{
    [Fact]
    public void Compare_identical_rules_has_empty_touched()
    {
        var rules = CreateRules(
            ("P1", ProgressionOutcome.Winner, StageId.New()),
            ("P2", ProgressionOutcome.Winner, StageId.New()));

        var diff = ProgressionRulesPathDiff.Compare(rules, rules.Copy());

        diff.Touched.Should().BeEmpty();
        diff.Added.Should().BeEmpty();
        diff.Removed.Should().BeEmpty();
        diff.Unchanged.Should().HaveCount(2);
        diff.HasStructuralPathChange.Should().BeFalse();
    }

    [Fact]
    public void Compare_detects_added_path_only()
    {
        var stageA = StageId.New();
        var before = CreateRules(("P1", ProgressionOutcome.Winner, stageA));
        var after = CreateRules(
            ("P1", ProgressionOutcome.Winner, stageA),
            ("P2", ProgressionOutcome.Winner, StageId.New()));

        var diff = ProgressionRulesPathDiff.Compare(before, after);

        diff.Touched.Should().BeEmpty();
        diff.Added.Should().ContainSingle(p => p.SourcePairKey == "P2");
        diff.HasStructuralPathChange.Should().BeFalse();
    }

    [Fact]
    public void Compare_detects_removed_path()
    {
        var stageA = StageId.New();
        var before = CreateRules(
            ("P1", ProgressionOutcome.Winner, stageA),
            ("P2", ProgressionOutcome.Winner, stageA));
        var after = CreateRules(("P1", ProgressionOutcome.Winner, stageA));

        var diff = ProgressionRulesPathDiff.Compare(before, after);

        diff.Touched.Should().ContainSingle(p => p.SourcePairKey == "P2");
        diff.Removed.Should().ContainSingle(p => p.SourcePairKey == "P2");
        diff.HasStructuralPathChange.Should().BeTrue();
    }

    [Fact]
    public void Compare_detects_destination_replacement_as_touched_before_path()
    {
        var stageA = StageId.New();
        var stageB = StageId.New();
        var before = CreateRules(("P1", ProgressionOutcome.Winner, stageA));
        var after = CreateRules(("P1", ProgressionOutcome.Winner, stageB));

        var diff = ProgressionRulesPathDiff.Compare(before, after);

        diff.Touched.Should().ContainSingle();
        diff.Touched[0].Destination.StageId.Should().Be(stageA);
        diff.ReplacedBefore.Should().ContainSingle(p => p.Destination.StageId == stageA);
        diff.Added.Should().BeEmpty();
    }

    [Fact]
    public void Compare_clear_after_marks_all_before_paths_removed()
    {
        var before = CreateRules(("P1", ProgressionOutcome.Winner, StageId.New()));

        var diff = ProgressionRulesPathDiff.Compare(before, afterRules: null);

        diff.Touched.Should().HaveCount(1);
        diff.Removed.Should().HaveCount(1);
    }

    private static ProgressionRules CreateRules(
        params (string PairKey, ProgressionOutcome Outcome, StageId DestinationStageId)[] specs)
    {
        var paths = specs
            .Select(s => new ProgressionPath(
                s.PairKey,
                s.Outcome,
                ProgressionDestination.ForPopulation(s.DestinationStageId)))
            .ToArray();
        return new ProgressionRules(paths);
    }
}
