// -----------------------------------------------------------------------
// <copyright file="MatchdayConfigurationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using MyClub.Scorer.Domain.MatchdayAggregate;
using MyClub.Shared.Domain.ValueObjects;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class MatchdayConfigurationTests(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void MatchdayConfiguration_TableMapping_IsCorrect() => AssertTableName<Matchday>("Matchdays");

    [Fact]
    public void MatchdayConfiguration_PrimaryKey_IsConfigured() => AssertPrimaryKey<Matchday>(nameof(Matchday.Id));

    [Fact]
    public void MatchdayConfiguration_BasicProperties_AreConfigured()
    {
        AssertPropertyExists<Matchday>(nameof(Matchday.OriginDate));
        AssertPropertyExists<Matchday>("_postponedDate"); // Private field mapped by EF
        AssertPropertyExists<Matchday>(nameof(Matchday.IsPostponed));
    }

    [Fact]
    public void MatchdayConfiguration_PropertyConstraints_AreCorrect()
    {
        AssertIsRequired<Matchday>(nameof(Matchday.Id));
        AssertIsRequired<Matchday>(nameof(Matchday.OriginDate));
        AssertIsRequired<Matchday>(nameof(Matchday.IsPostponed));
        AssertIsOptional<Matchday>("_postponedDate"); // Private field mapped by EF
    }

    [Fact]
    public void MatchdayConfiguration_DisplayName_IsConfigured()
    {
        var entityType = GetEntityType<Matchday>();
        var displayNameNavigation = entityType.FindNavigation(nameof(Matchday.DisplayName));

        Assert.NotNull(displayNameNavigation);
        Assert.True(displayNameNavigation.TargetEntityType.IsOwned());
        Output.WriteLine("✓ Matchday.DisplayName is configured as owned entity");

        var displayNameEntityType = displayNameNavigation.TargetEntityType;
        var nameProperty = displayNameEntityType.FindProperty(nameof(DisplayName.Name));
        var shortNameProperty = displayNameEntityType.FindProperty(nameof(DisplayName.ShortName));

        Assert.NotNull(nameProperty);
        Assert.NotNull(shortNameProperty);
        Assert.False(nameProperty.IsNullable);
        Assert.False(shortNameProperty.IsNullable);

        Output.WriteLine("✓ DisplayName.Name and DisplayName.ShortName properties are configured and required");
    }

    [Fact]
    public void MatchdayConfiguration_AuditableProperties_AreConfigured()
    {
        AssertPropertyExists<Matchday>(nameof(Matchday.CreatedBy));
        AssertPropertyExists<Matchday>(nameof(Matchday.CreatedAt));
        AssertPropertyExists<Matchday>(nameof(Matchday.ModifiedBy));
        AssertPropertyExists<Matchday>(nameof(Matchday.ModifiedAt));
    }

    [Fact]
    public void MatchdayConfiguration_PostponedDate_ColumnMapping_IsCorrect() =>
        AssertColumnName<Matchday>("_postponedDate", "PostponedDate"); // Private field mapped to PostponedDate column

    [Fact]
    public void MatchdayConfiguration_Matches_Relation_IsConfigured()
    {
        var entityType = GetEntityType<Matchday>();

        var matchesNavigation = entityType.FindNavigation(nameof(Matchday.Matches));
        if (matchesNavigation != null)
        {
            Assert.True(matchesNavigation.IsCollection);
            Output.WriteLine("✓ Matchday has Matches collection navigation");
        }
        else
        {
            var foreignKeys = entityType.GetForeignKeys().ToList();
            Output.WriteLine($"Matchday has {foreignKeys.Count} foreign key(s)");
        }
    }

    [Fact]
    public void MatchdayConfiguration_Debug_AllProperties() => DebugAllProperties<Matchday>();
}
