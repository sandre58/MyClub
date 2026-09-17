// -----------------------------------------------------------------------
// <copyright file="QualificationIntentReferenceCasesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

/// <summary>
/// RC1–RC4 from QualificationIntent decision, adapted to Qual V2 population destinations
/// (no SlotOrder / overrides — RC5 slot mapping is N/A).
/// </summary>
public sealed class QualificationIntentReferenceCasesTests
{
    [Fact]
    public void RC1_EachGroup_1_to_2_eight_groups_materializes_16_paths_one_intent()
    {
        var groups = Enumerable.Range(0, 8).Select(_ => GroupId.New()).ToArray();
        var dest = StageId.New();
        var intentId = IntentId.New();
        var intent = new QualificationIntent(
            intentId,
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 2,
            dest);

        var rules = QualificationRules.FromIntents([intent], groups);

        rules.Intents.Should().ContainSingle()
            .Which.Id.Should().Be(intentId);
        rules.Paths.Should().HaveCount(16);
        rules.Paths.Should().OnlyContain(p =>
            p.Destination.StageId.Equals(dest)
            && p.Selection.Mode == SelectionMode.Position
            && p.Source.Scope == RankingScope.Group);

        // A1, A2, B1, B2… (group then position)
        rules.Paths[0].Source.GroupId.Should().Be(groups[0]);
        rules.Paths[0].Selection.Value.Should().Be(1);
        rules.Paths[1].Source.GroupId.Should().Be(groups[0]);
        rules.Paths[1].Selection.Value.Should().Be(2);
        rules.Paths[2].Source.GroupId.Should().Be(groups[1]);
        rules.Paths[2].Selection.Value.Should().Be(1);
    }

    [Fact]
    public void RC2_Overall_1_to_8_materializes_8_paths_one_intent()
    {
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.Overall,
            1,
            8,
            dest);

        var rules = QualificationRules.FromIntents([intent], []);

        rules.Intents.Should().ContainSingle();
        rules.Paths.Should().HaveCount(8);
        rules.Paths.Select(p => p.Selection.Value).Should().Equal(1, 2, 3, 4, 5, 6, 7, 8);
        rules.Paths.Should().OnlyContain(p =>
            p.Source.Scope == RankingScope.Overall
            && p.Destination.StageId.Equals(dest));
    }

    [Fact]
    public void RC3_AcrossGroups_P3_k_1_to_2_materializes_2_paths_not_EachGroup()
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

        var rules = QualificationRules.FromIntents([intent], []);

        rules.Intents.Should().ContainSingle();
        rules.Paths.Should().HaveCount(2);
        rules.Paths.Should().OnlyContain(p =>
            p.Source.Scope == RankingScope.AcrossGroups
            && p.Source.AcrossGroupsPosition == 3
            && p.Destination.StageId.Equals(dest));
        rules.Paths.Select(p => p.Selection.Value).Should().Equal(1, 2);
    }

    [Fact]
    public void RC4_widening_range_1_2_to_1_3_rematerializes_without_slot_mapping()
    {
        var groups = new[] { GroupId.New(), GroupId.New() };
        var dest = StageId.New();
        var intentId = IntentId.New();
        var narrow = new QualificationIntent(
            intentId,
            1,
            QualificationIntentSourceKind.EachGroup,
            1,
            2,
            dest);

        var narrowRules = QualificationRules.FromIntents([narrow], groups);
        narrowRules.Paths.Should().HaveCount(4);

        var wide = new QualificationIntent(
            intentId,
            1,
            QualificationIntentSourceKind.EachGroup,
            1,
            3,
            dest);
        var wideRules = QualificationRules.FromIntents([wide], groups);

        wideRules.Intents.Should().ContainSingle().Which.Id.Should().Be(intentId);
        wideRules.Paths.Should().HaveCount(6);
        wideRules.Paths.Select(p => p.Selection.Value)
            .Should()
            .Equal(1, 2, 3, 1, 2, 3);
    }

    [Fact]
    public void Migration_path_only_Position_paths_become_singleton_intents()
    {
        var groupId = GroupId.New();
        var dest = StageId.New();
        var paths = new[]
        {
            new QualificationPath(
                1,
                QualificationSource.FromGroup(groupId),
                new QualificationSelection(SelectionMode.Position, 1),
                QualificationDestination.ForPopulation(dest)),
            new QualificationPath(
                2,
                QualificationSource.FromGroup(groupId),
                new QualificationSelection(SelectionMode.Position, 2),
                QualificationDestination.ForPopulation(dest))
        };

        var rules = new QualificationRules(paths);

        rules.Paths.Should().HaveCount(2);
        rules.Intents.Should().HaveCount(2);
        rules.Intents.Should().OnlyContain(i =>
            i.SourceKind == QualificationIntentSourceKind.SingleGroup
            && i.GroupId == groupId
            && i.PositionFrom == i.PositionTo
            && i.DestinationStageId.Equals(dest));
        rules.Intents.Select(i => i.PositionFrom).Should().Equal(1, 2);
        rules.Intents.Select(i => i.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Migration_non_Position_paths_do_not_produce_intents()
    {
        var dest = StageId.New();
        var path = new QualificationPath(
            1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Top, 4),
            QualificationDestination.ForPopulation(dest));

        var rules = new QualificationRules([path]);

        rules.Paths.Should().ContainSingle();
        rules.Intents.Should().BeEmpty();
    }

    [Fact]
    public void FromPersisted_keeps_intents_and_path_projection()
    {
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.Overall,
            1,
            2,
            dest);
        var built = QualificationRules.FromIntents([intent], []);

        var hydrated = QualificationRules.FromPersisted(built.Intents, built.Paths);

        hydrated.Intents.Should().ContainSingle().Which.Id.Should().Be(intent.Id);
        hydrated.Paths.Should().HaveCount(2);
        hydrated.Paths.Select(p => p.Selection.Value).Should().Equal(1, 2);
    }

    [Fact]
    public void ToSingletonIntent_Overall_Position_round_trips_kind()
    {
        var dest = StageId.New();
        var path = new QualificationPath(
            3,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 5),
            QualificationDestination.ForPopulation(dest));

        var intent = QualificationPathExpander.ToSingletonIntent(path);

        intent.Order.Should().Be(3);
        intent.SourceKind.Should().Be(QualificationIntentSourceKind.Overall);
        intent.PositionFrom.Should().Be(5);
        intent.PositionTo.Should().Be(5);
        intent.DestinationStageId.Should().Be(dest);
    }
}
