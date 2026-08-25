// -----------------------------------------------------------------------
// <copyright file="CliOptionsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.DevRunner;
using Xunit;

namespace MyClub.PlayUp.DevRunner.Tests;

public sealed class CliOptionsTests
{
    [Fact]
    public void Parse_templates_and_scenarios_with_progress()
    {
        var options = CliOptions.Parse(
        [
            "--reset",
            "--templates", "ligue-1:prepared,world-cup:finished",
            "--scenarios", "groups:running,cup:finished",
            "--seed", "7",
        ]);

        options.Reset.Should().BeTrue();
        options.Seed.Should().Be(7);
        options.Templates.Should().HaveCount(2);
        options.Templates[0].Should().Be(new SeedSpec("ligue-1", SeedProgress.Prepared));
        options.Scenarios.Should().HaveCount(2);
        options.Scenarios[1].Id.Should().Be("cup");
        options.Scenarios[1].Progress.Should().Be(SeedProgress.Finished);
    }
}
