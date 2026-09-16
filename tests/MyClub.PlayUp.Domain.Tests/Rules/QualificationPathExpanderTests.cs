// -----------------------------------------------------------------------
// <copyright file="QualificationPathExpanderTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class QualificationPathExpanderTests
{
    [Fact]
    public void EachGroup_expands_group_then_position_not_position_then_group()
    {
        var gA = GroupId.New();
        var gB = GroupId.New();
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 2,
            dest);

        var occurrences = QualificationPathExpander.Expand(intent, [gA, gB]);

        occurrences.Should().HaveCount(4);
        occurrences[0].Should().Be(QualificationSourceOccurrence.Group(gA, 1));
        occurrences[1].Should().Be(QualificationSourceOccurrence.Group(gA, 2));
        occurrences[2].Should().Be(QualificationSourceOccurrence.Group(gB, 1));
        occurrences[3].Should().Be(QualificationSourceOccurrence.Group(gB, 2));
    }

    [Fact]
    public void Materialize_zips_canonical_slots_in_order()
    {
        var gA = GroupId.New();
        var gB = GroupId.New();
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.EachGroup,
            1,
            2,
            dest);

        var paths = QualificationPathExpander.Materialize(
            [intent],
            [gA, gB],
            new Dictionary<StageId, IReadOnlyList<string>>
            {
                [dest] = ["QF1", "QF2", "QF3", "QF4"]
            });

        paths.Should().HaveCount(4);
        paths[0].Source.GroupId.Should().Be(gA);
        paths[0].Selection.Value.Should().Be(1);
        paths[0].Destination.SlotKey.Should().Be("QF1");
        paths[1].Destination.SlotKey.Should().Be("QF2");
        paths[2].Source.GroupId.Should().Be(gB);
        paths[3].Destination.SlotKey.Should().Be("QF4");
    }

    [Fact]
    public void Custom_override_replaces_zip_for_matching_occurrence()
    {
        var gA = GroupId.New();
        var dest = StageId.New();
        var occurrence = QualificationSourceOccurrence.Group(gA, 1);
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.SingleGroup,
            1,
            2,
            dest,
            QualificationMappingMode.Custom,
            groupId: gA,
            slotOverrides:
            [
                new QualificationSlotOverride(occurrence, "QF3")
            ]);

        var paths = QualificationPathExpander.Materialize(
            [intent],
            [gA],
            new Dictionary<StageId, IReadOnlyList<string>>
            {
                [dest] = ["QF1", "QF2", "QF3"]
            });

        paths[0].Destination.SlotKey.Should().Be("QF3");
        paths[1].Destination.SlotKey.Should().Be("QF2");
    }

    [Fact]
    public void AcrossGroups_expands_k_ascending_with_fixed_P()
    {
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.AcrossGroups,
            1,
            2,
            dest,
            acrossGroupsPosition: 3);

        var occurrences = QualificationPathExpander.Expand(intent, []);

        occurrences.Should().Equal(
            QualificationSourceOccurrence.AcrossGroups(3, 1),
            QualificationSourceOccurrence.AcrossGroups(3, 2));
    }

    [Fact]
    public void Materialize_hard_blocks_when_slots_insufficient()
    {
        var gA = GroupId.New();
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.EachGroup,
            1,
            2,
            dest);

        var act = () => QualificationPathExpander.Materialize(
            [intent],
            [gA, GroupId.New()],
            new Dictionary<StageId, IReadOnlyList<string>> { [dest] = ["QF1", "QF2"] });

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FromIntents_round_trips_through_QualificationRules()
    {
        GroupId.New();
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.Overall,
            1,
            3,
            dest);

        var rules = QualificationRules.FromIntents(
            [intent],
            [],
            new Dictionary<StageId, IReadOnlyList<string>>
            {
                [dest] = ["S1", "S2", "S3"]
            });

        rules.Intents.Should().ContainSingle();
        rules.Paths.Should().HaveCount(3);
        rules.Paths.Select(p => p.Selection.Value).Should().Equal(1, 2, 3);
    }
}
