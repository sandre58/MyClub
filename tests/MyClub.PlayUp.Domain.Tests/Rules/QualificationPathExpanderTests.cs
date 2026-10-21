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
    public void Materialize_EachGroup_targets_population_destination()
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

        var paths = QualificationPathExpander.Materialize([intent], [gA, gB]);

        paths.Should().HaveCount(4);
        paths.Should().OnlyContain(p => p.Destination.StageId.Equals(dest));
        paths[0].Source.GroupId.Should().Be(gA);
        paths[0].Selection.Value.Should().Be(1);
        paths[1].Selection.Value.Should().Be(2);
        paths[2].Source.GroupId.Should().Be(gB);
        paths[3].Selection.Value.Should().Be(2);
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
    public void Materialize_AcrossGroups_targets_population_destination()
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

        var paths = QualificationPathExpander.Materialize([intent], []);

        paths.Should().HaveCount(2);
        paths.Should().OnlyContain(p =>
            p.Destination.StageId.Equals(dest)
            && p.Source.Scope == RankingScope.AcrossGroups
            && p.Source.AcrossGroupsPosition == 3);
        paths.Select(p => p.Selection.Value).Should().Equal(1, 2);
    }

    [Fact]
    public void Materialize_Overall_targets_population_destination()
    {
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.Overall,
            1,
            3,
            dest);

        var paths = QualificationPathExpander.Materialize([intent], []);

        paths.Should().HaveCount(3);
        paths.Should().OnlyContain(p =>
            p.Destination.StageId.Equals(dest)
            && p.Source.Scope == RankingScope.Overall
            && p.Selection.Mode == SelectionMode.Position);
        paths.Select(p => p.Selection.Value).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void FromIntents_round_trips_through_QualificationRules()
    {
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.Overall,
            1,
            3,
            dest);

        var rules = QualificationRules.FromIntents([intent], []);

        rules.Intents.Should().ContainSingle();
        rules.Paths.Should().HaveCount(3);
        rules.Paths.Select(p => p.Selection.Value).Should().Equal(1, 2, 3);
        rules.Paths.Should().OnlyContain(p => p.Destination.StageId.Equals(dest));
    }

    [Fact]
    public void Materialize_Place_intent_uses_ForSlot()
    {
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.Overall,
            1,
            1,
            dest,
            destinationSlotKeys: ["R16-1"]);

        var paths = QualificationPathExpander.Materialize([intent], []);

        paths.Should().ContainSingle();
        paths[0].Destination.TargetsPopulation.Should().BeFalse();
        paths[0].Destination.SlotKey.Should().Be("R16-1");
    }

    [Fact]
    public void Materialize_Place_EachGroup_zips_slot_keys_to_occurrences()
    {
        var gA = GroupId.New();
        var gB = GroupId.New();
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.EachGroup,
            1,
            1,
            dest,
            destinationSlotKeys: ["R16-1", "R16-2"]);

        var paths = QualificationPathExpander.Materialize([intent], [gA, gB]);

        paths.Should().HaveCount(2);
        paths[0].Source.GroupId.Should().Be(gA);
        paths[0].Destination.SlotKey.Should().Be("R16-1");
        paths[1].Source.GroupId.Should().Be(gB);
        paths[1].Destination.SlotKey.Should().Be("R16-2");
    }

    [Fact]
    public void Materialize_rejects_Place_when_slot_key_count_mismatches_Expand()
    {
        var gA = GroupId.New();
        var gB = GroupId.New();
        var dest = StageId.New();
        var intent = new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.EachGroup,
            1,
            1,
            dest,
            destinationSlotKeys: ["R16-1"]);

        var act = () => QualificationPathExpander.Materialize([intent], [gA, gB]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_duplicate_Place_slot_keys()
    {
        var dest = StageId.New();
        var act = () => new QualificationIntent(
            IntentId.New(),
            1,
            QualificationIntentSourceKind.Overall,
            1,
            2,
            dest,
            destinationSlotKeys: ["R16-1", "R16-1"]);

        act.Should().Throw<DomainException>()
            .Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }
}
