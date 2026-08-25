// -----------------------------------------------------------------------
// <copyright file="GeneratorDeterminismTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Development.Generators;
using MyClub.PlayUp.Development.Runtime;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

public sealed class GeneratorDeterminismTests
{
    [Fact]
    public void Same_seed_and_scenario_same_team_names_and_scores()
    {
        var e1 = new DeterministicEntropy("group-stage-mid", 42);
        var e2 = new DeterministicEntropy("group-stage-mid", 42);

        for (var i = 0; i < 8; i++)
        {
            TeamNameGenerator.Create(e1, i).Should().Be(TeamNameGenerator.Create(e2, i));
            ScoreGenerator.Create(e1).Should().Be(ScoreGenerator.Create(e2));
        }
    }

    [Fact]
    public void Different_seed_changes_sequence()
    {
        var a = new DeterministicEntropy("s", 1);
        var b = new DeterministicEntropy("s", 2);
        var scoresA = Enumerable.Range(0, 20).Select(_ => ScoreGenerator.Create(a)).ToArray();
        var scoresB = Enumerable.Range(0, 20).Select(_ => ScoreGenerator.Create(b)).ToArray();
        scoresA.Should().NotEqual(scoresB);
    }

    [Fact]
    public void Different_scenario_has_independent_sequence()
    {
        var a = new DeterministicEntropy("alpha", 42);
        var b = new DeterministicEntropy("beta", 42);
        ScoreGenerator.Create(a).Should().NotBe(ScoreGenerator.Create(b));
    }
}
