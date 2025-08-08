// -----------------------------------------------------------------------
// <copyright file="StageConfigurationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.StageAggregate;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class StageConfigurationTests(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void StageConfiguration_TableMapping_IsCorrect() => AssertTableName<Stage>("Stages");

    [Fact]
    public void StageConfiguration_PrimaryKey_IsConfigured() => AssertPrimaryKey<Stage>(nameof(Stage.Id));

    [Fact]
    public void StageConfiguration_DisplayName_IsConfigured()
    {
        // DisplayName is owned entity with separate table, verify navigation exists
        var entityType = GetEntityType<Stage>();
        var displayNameNavigation = entityType.FindNavigation(nameof(Stage.DisplayName));
        Assert.NotNull(displayNameNavigation);
        Assert.True(displayNameNavigation.TargetEntityType.IsOwned());
    }

    [Fact]
    public void StageConfiguration_PropertyConstraints_AreCorrect()
    {
        AssertIsRequired<Stage>(nameof(Stage.Id));
        AssertIsRequired<Stage>(nameof(Stage.IsConsolation));

        // Auditable properties constraints
        AssertPropertyExists<Stage>(nameof(Stage.CreatedBy));
        AssertPropertyExists<Stage>(nameof(Stage.CreatedAt));
        AssertPropertyExists<Stage>(nameof(Stage.ModifiedBy));
        AssertPropertyExists<Stage>(nameof(Stage.ModifiedAt));
    }

    [Fact]
    public void StageConfiguration_AuditableProperties_AreConfigured()
    {
        AssertPropertyExists<Stage>(nameof(Stage.CreatedBy));
        AssertPropertyExists<Stage>(nameof(Stage.CreatedAt));
        AssertPropertyExists<Stage>(nameof(Stage.ModifiedBy));
        AssertPropertyExists<Stage>(nameof(Stage.ModifiedAt));
    }

    [Fact]
    public void StageConfiguration_Debug_AllProperties() => DebugAllProperties<Stage>();
}
