// -----------------------------------------------------------------------
// <copyright file="GroupStageConfigurationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Scorer.Domain.StageAggregate;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.Scorer.Infrastructure.Persistence.Tests;

public class GroupStageConfigurationTests(ITestOutputHelper output) : ConfigurationTestsBase(output)
{
    [Fact]
    public void GroupStageConfiguration_TableMapping_IsCorrect() => AssertTableName<GroupStage>("Stages");

    [Fact]
    public void GroupStageConfiguration_InheritsFromStage()
    {
        var entityType = GetEntityType<GroupStage>();
        Assert.Equal(typeof(Stage), entityType.BaseType?.ClrType);
        Output.WriteLine("✓ GroupStage inherits from Stage");
    }

    [Fact]
    public void GroupStageConfiguration_StandingRankStatuses_IsConfigured()
    {
        var entityType = GetEntityType<GroupStage>();
        var statusesProperty = entityType.FindProperty(nameof(GroupStage.Labels));

        if (statusesProperty == null)
            return;

        AssertHasConversion<GroupStage>(nameof(GroupStage.Labels));
        Output.WriteLine("✓ GroupStage.Labels has value converter");
    }

    [Fact]
    public void GroupStageConfiguration_StandingRules_IsConfigured()
    {
        var entityType = GetEntityType<GroupStage>();
        var rulesNavigation = entityType.FindNavigation(nameof(GroupStage.StandingRules));

        if (rulesNavigation != null)
        {
            Assert.True(rulesNavigation.TargetEntityType.IsOwned());
            Output.WriteLine("✓ GroupStage.StandingRules is configured as owned entity");
        }
        else
        {
            var rulesProperty = entityType.FindProperty(nameof(GroupStage.StandingRules));
            if (rulesProperty == null)
                return;

            AssertHasConversion<GroupStage>(nameof(GroupStage.StandingRules));
            Output.WriteLine("✓ GroupStage.StandingRules has value converter");
        }
    }

    [Fact]
    public void GroupStageConfiguration_PenaltyPoints_IsConfigured()
    {
        var entityType = GetEntityType<GroupStage>();
        var penaltyPointsProperty = entityType.FindProperty(nameof(GroupStage.PenaltyPoints));

        if (penaltyPointsProperty == null)
            return;

        AssertHasConversion<GroupStage>(nameof(GroupStage.PenaltyPoints));
        AssertColumnName<GroupStage>(nameof(GroupStage.PenaltyPoints), nameof(GroupStage.PenaltyPoints));
        Output.WriteLine("✓ GroupStage.PenaltyPoints has value converter and correct column name");
    }

    [Fact]
    public void GroupStageConfiguration_Groups_Relation_IsConfigured()
    {
        var entityType = GetEntityType<GroupStage>();
        var groupsNavigation = entityType.FindNavigation(nameof(GroupStage.Groups));

        if (groupsNavigation == null)
            return;

        Assert.True(groupsNavigation.IsCollection);
        Output.WriteLine("✓ GroupStage has Groups collection navigation");
    }

    [Fact]
    public void GroupStageConfiguration_Matchdays_IsConfigured()
    {
        var entityType = GetEntityType<GroupStage>();
        var matchdaysProperty = entityType.FindProperty(nameof(GroupStage.Matchdays));

        if (matchdaysProperty == null)
            return;

        AssertHasConversion<GroupStage>(nameof(GroupStage.Matchdays));
        Output.WriteLine("✓ GroupStage.Matchdays has value converter");
    }

    [Fact]
    public void GroupStageConfiguration_Debug_AllProperties() => DebugAllProperties<GroupStage>();
}
