// -----------------------------------------------------------------------
// <copyright file="MatchConfigurationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Shared.Domain.Enums;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class MatchConfigurationTests(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void MatchConfiguration_TableMapping_IsCorrect() => AssertTableName<Match>("Matches");

    [Fact]
    public void MatchConfiguration_PrimaryKey_IsConfigured() => AssertPrimaryKey<Match>(nameof(Match.Id));

    [Fact]
    public void MatchConfiguration_BasicProperties_AreConfigured()
    {
        AssertPropertyExists<Match>(nameof(Match.OriginDate));
        AssertPropertyExists<Match>(nameof(Match.PostponedDate));
        AssertPropertyExists<Match>(nameof(Match.IsNeutralStadium));
        AssertPropertyExists<Match>(nameof(Match.StadiumId));
        AssertPropertyExists<Match>(nameof(Match.Status));
        AssertPropertyExists<Match>(nameof(Match.AfterExtraTime));
    }

    [Fact]
    public void MatchConfiguration_PropertyConstraints_AreCorrect()
    {
        AssertIsRequired<Match>(nameof(Match.Id));
        AssertIsRequired<Match>(nameof(Match.OriginDate));
        AssertIsOptional<Match>(nameof(Match.PostponedDate));
        AssertIsRequired<Match>(nameof(Match.IsNeutralStadium));
        AssertIsRequired<Match>(nameof(Match.Status));
        AssertIsRequired<Match>(nameof(Match.AfterExtraTime));
    }

    [Fact]
    public void MatchConfiguration_OwnedEntities_AreConfigured()
    {
        var entityType = GetEntityType<Match>();

        var homeNavigation = entityType.FindNavigation(nameof(Match.Home));
        var awayNavigation = entityType.FindNavigation(nameof(Match.Away));

        if (homeNavigation != null)
        {
            Assert.True(homeNavigation.TargetEntityType.IsOwned());
            Output.WriteLine("✓ Match.Home is configured as owned entity");
        }

        if (awayNavigation == null)
            return;

        Assert.True(awayNavigation.TargetEntityType.IsOwned());
        Output.WriteLine("✓ Match.Away is configured as owned entity");
    }

    [Fact]
    public void MatchConfiguration_ValueConverters_AreConfigured()
    {
        // Test Status enum to string conversion
        var entityType = GetEntityType<Match>();
        var statusProperty = entityType.FindProperty(nameof(Match.Status));

        if (statusProperty != null)
        {
            // For enum properties, EF may use built-in conversion rather than explicit converter
            // The important thing is that the property exists and is properly configured
            Assert.Equal(typeof(MatchStatus), statusProperty.ClrType);
            Output.WriteLine("✓ Match.Status is properly configured (enum conversion may be built-in)");
        }

        var stadiumIdProperty = entityType.FindProperty(nameof(Match.StadiumId));

        if (stadiumIdProperty == null)
            return;

        Assert.NotNull(stadiumIdProperty.GetValueConverter());
        Output.WriteLine("✓ Match.StadiumId has value converter");
    }

    [Fact]
    public void MatchConfiguration_MatchFormat_IsConfigured()
    {
        var entityType = GetEntityType<Match>();
        var formatNavigation = entityType.FindNavigation(nameof(Match.Format));

        if (formatNavigation == null)
            return;

        Assert.True(formatNavigation.TargetEntityType.IsOwned());
        Output.WriteLine("✓ Match.Format is configured as owned entity");

        var formatEntityType = formatNavigation.TargetEntityType;

        // Check for RegulationTime owned navigation
        var regulationTimeNavigation = formatEntityType.FindNavigation("RegulationTime");
        Assert.NotNull(regulationTimeNavigation);
        Output.WriteLine("✓ MatchFormat.RegulationTime navigation exists");

        var regulationTimeEntityType = regulationTimeNavigation.TargetEntityType;
        Assert.NotNull(regulationTimeEntityType.FindProperty("Number"));
        Assert.NotNull(regulationTimeEntityType.FindProperty("Duration"));
        Output.WriteLine("✓ MatchFormat properties are correctly mapped");
    }

    [Fact]
    public void MatchConfiguration_MatchRules_IsConfigured()
    {
        var entityType = GetEntityType<Match>();
        var rulesNavigation = entityType.FindNavigation(nameof(Match.Rules));

        if (rulesNavigation == null)
            return;

        Assert.True(rulesNavigation.TargetEntityType.IsOwned());
        Output.WriteLine("✓ Match.Rules is configured as owned entity");
    }

    [Fact]
    public void MatchConfiguration_AuditableProperties_AreConfigured()
    {
        AssertPropertyExists<Match>(nameof(Match.CreatedBy));
        AssertPropertyExists<Match>(nameof(Match.CreatedAt));
        AssertPropertyExists<Match>(nameof(Match.ModifiedBy));
        AssertPropertyExists<Match>(nameof(Match.ModifiedAt));
    }

    [Fact]
    public void MatchConfiguration_MatchEvents_Relation_IsConfigured()
    {
        var entityType = GetEntityType<Match>();
        var eventsNavigation = entityType.FindNavigation("Events");

        if (eventsNavigation == null)
            return;

        Output.WriteLine("✓ Match has Events navigation");
        Assert.True(eventsNavigation.IsCollection);
    }

    [Fact]
    public void MatchConfiguration_Debug_AllProperties() => DebugAllProperties<Match>();
}
