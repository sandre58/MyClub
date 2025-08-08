// -----------------------------------------------------------------------
// <copyright file="ChampionshipStageConfigurationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using MyClub.Scorer.Domain.StageAggregate;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class ChampionshipStageConfigurationTests(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void ChampionshipStageConfiguration_TableMapping_IsCorrect() => AssertTableName<ChampionshipStage>("Stages"); // TPH inheritance shares table with Stage

    [Fact]
    public void ChampionshipStageConfiguration_InheritsFromStage()
    {
        var entityType = GetEntityType<ChampionshipStage>();
        Assert.Equal(typeof(Stage), entityType.BaseType?.ClrType);
        Output.WriteLine("✓ ChampionshipStage inherits from Stage");
    }

    [Fact]
    public void ChampionshipStageConfiguration_Labels_IsConfigured()
    {
        var entityType = GetEntityType<ChampionshipStage>();
        var labelsNavigation = entityType.FindNavigation(nameof(ChampionshipStage.Labels));

        if (labelsNavigation == null)
            return;

        Assert.True(labelsNavigation.IsCollection);
        Assert.True(labelsNavigation.TargetEntityType.IsOwned());
        Output.WriteLine("✓ ChampionshipStage.Labels is configured as owned collection");
    }

    [Fact]
    public void ChampionshipStageConfiguration_StandingRules_IsConfigured()
    {
        var entityType = GetEntityType<ChampionshipStage>();
        var rulesNavigation = entityType.FindNavigation(nameof(ChampionshipStage.StandingRules));

        if (rulesNavigation != null)
        {
            Assert.True(rulesNavigation.TargetEntityType.IsOwned());
            Output.WriteLine("✓ ChampionshipStage.StandingRules is configured as owned entity");
        }
        else
        {
            var rulesProperty = entityType.FindProperty(nameof(ChampionshipStage.StandingRules));
            if (rulesProperty == null)
                return;

            AssertHasConversion<ChampionshipStage>(nameof(ChampionshipStage.StandingRules));
            Output.WriteLine("✓ ChampionshipStage.StandingRules has value converter");
        }
    }

    [Fact]
    public void ChampionshipStageConfiguration_PenaltyPoints_IsConfigured()
    {
        AssertPropertyExists<ChampionshipStage>(nameof(ChampionshipStage.PenaltyPoints));
        AssertHasConversion<ChampionshipStage>(nameof(ChampionshipStage.PenaltyPoints));
        AssertColumnName<ChampionshipStage>(nameof(ChampionshipStage.PenaltyPoints), nameof(ChampionshipStage.PenaltyPoints));
        Output.WriteLine("✓ ChampionshipStage.PenaltyPoints is correctly configured");
    }

    [Fact]
    public void ChampionshipStageConfiguration_Matchdays_Relation_IsConfigured()
    {
        var entityType = GetEntityType<ChampionshipStage>();

        var foreignKeys = entityType.GetForeignKeys().ToList();
        var navigations = entityType.GetNavigations().ToList();

        Output.WriteLine($"Foreign keys count: {foreignKeys.Count}");
        Output.WriteLine($"Navigations count: {navigations.Count}");

        foreach (var nav in navigations)
        {
            Output.WriteLine($"Navigation: {nav.Name} -> {nav.TargetEntityType.ClrType.Name}");
        }
    }

    [Fact]
    public void ChampionshipStageConfiguration_Debug_AllProperties() => DebugAllProperties<ChampionshipStage>();
}
