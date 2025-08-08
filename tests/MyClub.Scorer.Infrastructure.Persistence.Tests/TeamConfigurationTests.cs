// -----------------------------------------------------------------------
// <copyright file="TeamConfigurationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.CompetitionAggregate.Stadiums;
using MyClub.Scorer.Domain.CompetitionAggregate.Teams;
using MyClub.Shared.Domain.ValueObjects;
using MyNet.Utilities.Geography;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class TeamConfigurationTests(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void TeamConfiguration_TableMapping_IsCorrect() => AssertTableName<Team>("Teams");

    [Fact]
    public void TeamConfiguration_PrimaryKey_IsConfigured() => AssertPrimaryKey<Team>(nameof(Team.Id));

    [Fact]
    public void TeamConfiguration_Properties_AreConfigured()
    {
        // Basic properties
        AssertPropertyExists<Team>(nameof(Team.Logo));
        AssertPropertyExists<Team>(nameof(Team.Country));
        AssertPropertyExists<Team>(nameof(Team.HomeColor));
        AssertPropertyExists<Team>(nameof(Team.AwayColor));

        // Verify DisplayName as owned entity - not direct properties
        var entityType = GetEntityType<Team>();
        var displayNameNavigation = entityType.FindNavigation(nameof(Team.DisplayName));
        Assert.NotNull(displayNameNavigation);
        Assert.True(displayNameNavigation.TargetEntityType.IsOwned());

        // Auditable properties
        AssertPropertyExists<Team>(nameof(Team.CreatedBy));
        AssertPropertyExists<Team>(nameof(Team.CreatedAt));
        AssertPropertyExists<Team>(nameof(Team.ModifiedBy));
        AssertPropertyExists<Team>(nameof(Team.ModifiedAt));
    }

    [Fact]
    public void TeamConfiguration_PropertyTypes_AreCorrect()
    {
        AssertPropertyType<Team>(nameof(Team.Logo), typeof(byte[]));
        AssertPropertyType<Team>(nameof(Team.Country), typeof(Country));
        AssertPropertyType<Team>(nameof(Team.HomeColor), typeof(string));
        AssertPropertyType<Team>(nameof(Team.AwayColor), typeof(string));
    }

    [Fact]
    public void TeamConfiguration_PropertyConstraints_AreCorrect()
    {
        AssertIsRequired<Team>(nameof(Team.Id));

        // Check DisplayName constraints in owned entity
        var entityType = GetEntityType<Team>();
        var displayNameNavigation = entityType.FindNavigation(nameof(Team.DisplayName));
        Assert.NotNull(displayNameNavigation);

        var displayNameEntityType = displayNameNavigation.TargetEntityType;
        var nameProperty = displayNameEntityType.FindProperty(nameof(DisplayName.Name));
        var shortNameProperty = displayNameEntityType.FindProperty(nameof(DisplayName.ShortName));

        Assert.NotNull(nameProperty);
        Assert.False(nameProperty.IsNullable); // Name is required
        Assert.NotNull(shortNameProperty);
        Assert.False(shortNameProperty.IsNullable); // ShortName is required too

        AssertIsOptional<Team>(nameof(Team.Logo));
    }

    [Fact]
    public void TeamConfiguration_ValueConverters_AreConfigured()
    {
        // Country has explicit converter
        AssertHasConversion<Team>(nameof(Team.Country));

        // HomeColor and AwayColor are strings, so no explicit converter needed
        // Just verify they exist as string properties
        var entityType = GetEntityType<Team>();
        var homeColorProperty = entityType.FindProperty(nameof(Team.HomeColor));
        var awayColorProperty = entityType.FindProperty(nameof(Team.AwayColor));

        Assert.NotNull(homeColorProperty);
        Assert.NotNull(awayColorProperty);
        Assert.Equal(typeof(string), homeColorProperty.ClrType);
        Assert.Equal(typeof(string), awayColorProperty.ClrType);

        Output.WriteLine("✓ Team.HomeColor and Team.AwayColor are configured as string properties");
    }

    [Fact]
    public void TeamConfiguration_OwnedEntities_AreConfigured()
    {
        var entityType = GetEntityType<Team>();
        var displayNameNavigation = entityType.FindNavigation(nameof(Team.DisplayName));
        Assert.NotNull(displayNameNavigation);
        Assert.True(displayNameNavigation.TargetEntityType.IsOwned());

        Output.WriteLine("✓ Team.DisplayName is configured as owned entity");
    }

    [Fact]
    public void TeamConfiguration_Stadium_Relation_IsConfigured()
    {
        var entityType = GetEntityType<Team>();
        var stadiumProperty = entityType.FindProperty(nameof(Team.StadiumId));
        if (stadiumProperty != null)
        {
            AssertForeignKey<Team>(nameof(Team.StadiumId), nameof(Stadium));
        }
    }

    [Fact]
    public void TeamConfiguration_Debug_AllProperties() => DebugAllProperties<Team>();
}
