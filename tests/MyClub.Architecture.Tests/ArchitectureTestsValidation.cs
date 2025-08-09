// -----------------------------------------------------------------------
// <copyright file="ArchitectureTestsValidation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Infrastructure.Persistence.DbContexts;
using Xunit;

namespace MyClub.Architecture.Tests;

/// <summary>
/// Simple validation test to ensure our architecture tests are working correctly.
/// </summary>
public class ArchitectureTestsValidation
{
    [Fact]
    public void Architecture_Test_Assemblies_Should_Load_Successfully()
    {
        // Arrange & Act
        var domainAssembly = typeof(Competition).Assembly;
        var applicationAssembly = typeof(Scorer.Application.Competitions.Commands.AddTeam.AddTeamCommand).Assembly;
        var infrastructureAssembly = typeof(ScorerDbContext).Assembly;

        // Assert
        domainAssembly.Should().NotBeNull("Domain assembly should load successfully");
        applicationAssembly.Should().NotBeNull("Application assembly should load successfully");
        infrastructureAssembly.Should().NotBeNull("Infrastructure assembly should load successfully");

        // Verify basic types exist
        domainAssembly.GetType("MyClub.Scorer.Domain.CompetitionAggregate.Competition").Should().NotBeNull();
        applicationAssembly.GetType("MyClub.Scorer.Application.Competitions.Commands.AddTeam.AddTeamCommand").Should().NotBeNull();
        infrastructureAssembly.GetType("MyClub.Scorer.Infrastructure.Persistence.DbContexts.ScorerDbContext").Should().NotBeNull();
    }

    [Fact]
    public void NetArchTest_Framework_Should_Work()
    {
        // This is a simple test to ensure NetArchTest is working properly
        var domainAssembly = typeof(Competition).Assembly;

        var types = NetArchTest.Rules.Types.InAssembly(domainAssembly)
            .That()
            .AreClasses()
            .GetTypes();

        types.Should().NotBeEmpty("Should find classes in the domain assembly");
    }
}
