// -----------------------------------------------------------------------
// <copyright file="QualificationDestinationForFormTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using Xunit;

namespace MyClub.PlayUp.Domain.Tests.Rules;

public sealed class QualificationDestinationForFormTests
{
    [Fact]
    public void ForForm_targets_form_only()
    {
        var stageId = StageId.New();

        var dest = QualificationDestination.ForForm(stageId);

        dest.TargetsForm.Should().BeTrue();
        dest.TargetsPopulation.Should().BeFalse();
        dest.TargetsSlot.Should().BeFalse();
        dest.TargetsGroup.Should().BeFalse();
        dest.Form.Should().BeTrue();
        dest.SlotKey.Should().BeNull();
        dest.GroupId.Should().BeNull();
        dest.StageId.Should().Be(stageId);
    }

    [Fact]
    public void ForPopulation_is_not_form()
    {
        var dest = QualificationDestination.ForPopulation(StageId.New());

        dest.TargetsPopulation.Should().BeTrue();
        dest.TargetsForm.Should().BeFalse();
        dest.Form.Should().BeFalse();
    }

    [Fact]
    public void Constructor_rejects_form_with_slot()
    {
        var act = () => new QualificationDestination(StageId.New(), "SF1-A", groupId: null, form: true);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Constructor_rejects_form_with_group()
    {
        var act = () => new QualificationDestination(StageId.New(), slotKey: null, GroupId.New(), form: true);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Intent_rejects_form_with_slot_keys()
    {
        var act = () => new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.Overall,
            positionFrom: 1,
            positionTo: 1,
            StageId.New(),
            destinationSlotKeys: ["SF1-A"],
            destinationForm: true);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Intent_rejects_form_with_group_ids()
    {
        var act = () => new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.Overall,
            positionFrom: 1,
            positionTo: 1,
            StageId.New(),
            destinationGroupIds: [GroupId.New()],
            destinationForm: true);

        act.Should().Throw<DomainException>().Which.Code.Should().Be(RulesErrorCodes.QualificationRulesInvalid);
    }

    [Fact]
    public void Expander_emits_ForForm_on_every_path()
    {
        var destStage = StageId.New();
        var sourceGroupOrder = new[] { GroupId.New(), GroupId.New() };
        var intent = new QualificationIntent(
            IntentId.New(),
            order: 1,
            QualificationIntentSourceKind.EachGroup,
            positionFrom: 1,
            positionTo: 1,
            destStage,
            destinationForm: true);

        var paths = QualificationPathExpander.Materialize([intent], sourceGroupOrder);

        paths.Should().HaveCount(2);
        paths.Should().OnlyContain(p => p.Destination.Equals(QualificationDestination.ForForm(destStage)));
    }

    [Fact]
    public void ToSingletonIntent_roundtrips_ForForm()
    {
        var path = new QualificationPath(
            order: 1,
            QualificationSource.Overall(),
            new QualificationSelection(SelectionMode.Position, 1),
            QualificationDestination.ForForm(StageId.New()));

        var intent = QualificationPathExpander.ToSingletonIntent(path);

        intent.TargetsForm.Should().BeTrue();
        intent.TargetsPopulation.Should().BeFalse();
        intent.DestinationForm.Should().BeTrue();
        intent.DestinationSlotKeys.Should().BeEmpty();
        intent.DestinationGroupIds.Should().BeEmpty();
    }
}
