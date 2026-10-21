// -----------------------------------------------------------------------
// <copyright file="RegulationPacksTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using Xunit;

namespace MyClub.PlayUp.TestKit.Tests;

public sealed class RegulationPacksTests
{
    [Fact]
    public void Standard_matches_BootstrapRegulation_baseline()
    {
        var pack = RegulationPacks.Standard();
        var bootstrap = BootstrapRegulation.Standard();

        pack.EntryRules.MinimumTeams.Should().Be(bootstrap.EntryRules.MinimumTeams);
        pack.EntryRules.MaximumTeams.Should().Be(bootstrap.EntryRules.MaximumTeams);
        pack.MatchRules.Duration.Should().Be(bootstrap.MatchRules.Duration);
        pack.StandingRules.Points.Should().Be(bootstrap.StandingRules.Points);
        pack.StandingRules.RankingCriteria.Should().Equal(bootstrap.StandingRules.RankingCriteria);
    }
}
