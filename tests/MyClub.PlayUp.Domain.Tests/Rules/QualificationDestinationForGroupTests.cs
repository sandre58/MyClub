// -----------------------------------------------------------------------
// <copyright file="QualificationDestinationForGroupTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class QualificationDestinationForGroupTests
{
    [Fact]
    public void ForGroup_targets_group_only()
    {
        var stageId = StageId.New();
        var groupId = GroupId.New();

        var dest = QualificationDestination.ForGroup(stageId, groupId);

        dest.TargetsGroup.Should().BeTrue();
        dest.TargetsSlot.Should().BeFalse();
        dest.TargetsPopulation.Should().BeFalse();
        dest.GroupId.Should().Be(groupId);
        dest.SlotKey.Should().BeNull();
    }

    [Fact]
    public void Constructor_rejects_slot_and_group_together()
    {
        var act = () => new QualificationDestination(StageId.New(), "SF1-A", GroupId.New());

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Intent_rejects_slot_keys_and_group_ids_together()
    {
        var act = () => new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.Overall,
            positionFrom: 1,
            positionTo: 1,
            StageId.New(),
            destinationSlotKeys: ["SF1-A"],
            destinationGroupIds: [GroupId.New()]);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Expander_zips_DestinationGroupIds_onto_paths()
    {
        var destStage = StageId.New();
        var groupA = GroupId.New();
        var groupB = GroupId.New();
        var sourceGroupOrder = new[] { GroupId.New(), GroupId.New() };
        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            destStage,
            destinationGroupIds: [groupA, groupB]);

        var paths = QualificationPathExpander.Materialize([intent], sourceGroupOrder);

        paths.Should().HaveCount(2);
        paths[0].Destination.Should().BeEquivalentTo(QualificationDestination.ForGroup(destStage, groupA));
        paths[1].Destination.Should().BeEquivalentTo(QualificationDestination.ForGroup(destStage, groupB));
    }

    [Fact]
    public void Rules_allow_duplicate_group_destinations()
    {
        var destStage = StageId.New();
        var groupA = GroupId.New();
        var sourceGroupOrder = new[] { GroupId.New(), GroupId.New() };
        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            destStage,
            destinationGroupIds: [groupA, groupA]);

        var act = () => QualificationRules.FromIntents([intent], sourceGroupOrder);

        act.Should().NotThrow();
        var rules = QualificationRules.FromIntents([intent], sourceGroupOrder);
        rules.Paths.Should().HaveCount(2);
        rules.Paths.Should().OnlyContain(p => p.Destination.GroupId == groupA);
    }
}
