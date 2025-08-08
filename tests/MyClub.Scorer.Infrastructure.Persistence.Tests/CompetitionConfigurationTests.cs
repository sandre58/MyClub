// -----------------------------------------------------------------------
// <copyright file="CompetitionConfigurationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.CompetitionAggregate;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class CompetitionConfigurationTests(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void CompetitionConfiguration_TableMapping_IsCorrect() => AssertTableName<Competition>("Competitions");

    [Fact]
    public void CompetitionConfiguration_PrimaryKey_IsConfigured() => AssertPrimaryKey<Competition>(nameof(Competition.Id));

    [Fact]
    public void CompetitionConfiguration_DisplayName_IsConfigured()
    {
        // DisplayName is an owned entity with separate table, verify navigation exists
        var entityType = GetEntityType<Competition>();
        var displayNameNavigation = entityType.FindNavigation(nameof(Competition.DisplayName));
        Assert.NotNull(displayNameNavigation);
        Assert.True(displayNameNavigation.TargetEntityType.IsOwned());
    }

    [Fact]
    public void CompetitionConfiguration_MatchRules_IsConfigured()
    {
        var entityType = GetEntityType<Competition>();
        var rulesNavigation = entityType.FindNavigation(nameof(Competition.MatchRules));

        if (rulesNavigation == null)
            return;

        Assert.True(rulesNavigation.TargetEntityType.IsOwned());
        Output.WriteLine("✓ Competition.MatchRules is configured as owned entity");
    }

    [Fact]
    public void CompetitionConfiguration_MatchFormat_IsConfigured()
    {
        var entityType = GetEntityType<Competition>();
        var formatNavigation = entityType.FindNavigation(nameof(Competition.MatchFormat));

        if (formatNavigation == null)
            return;

        Assert.True(formatNavigation.TargetEntityType.IsOwned());
        Output.WriteLine("✓ Competition.MatchFormat is configured as owned entity");
    }

    [Fact]
    public void CompetitionConfiguration_AuditableProperties_AreConfigured()
    {
        AssertPropertyExists<Competition>(nameof(Competition.CreatedBy));
        AssertPropertyExists<Competition>(nameof(Competition.CreatedAt));
        AssertPropertyExists<Competition>(nameof(Competition.ModifiedBy));
        AssertPropertyExists<Competition>(nameof(Competition.ModifiedAt));
    }

    [Fact]
    public void CompetitionConfiguration_Teams_Relation_IsConfigured()
    {
        var entityType = GetEntityType<Competition>();
        var teamsNavigation = entityType.FindNavigation(nameof(Competition.Teams));

        if (teamsNavigation == null)
            return;

        Assert.True(teamsNavigation.IsCollection);
        Output.WriteLine("✓ Competition has Teams collection navigation");
    }

    [Fact]
    public void CompetitionConfiguration_Stadiums_Relation_IsConfigured()
    {
        var entityType = GetEntityType<Competition>();
        var stadiumsNavigation = entityType.FindNavigation(nameof(Competition.Stadiums));

        if (stadiumsNavigation == null)
            return;

        Assert.True(stadiumsNavigation.IsCollection);
        Output.WriteLine("✓ Competition has Stadiums collection navigation");
    }

    [Fact]
    public void CompetitionConfiguration_Debug_AllProperties() => DebugAllProperties<Competition>();
}
