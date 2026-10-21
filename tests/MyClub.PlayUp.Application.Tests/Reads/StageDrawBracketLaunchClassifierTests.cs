// -----------------------------------------------------------------------
// <copyright file="StageDrawBracketLaunchClassifierTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StageDrawBracketLaunchClassifierTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void TryClassify_returns_null_without_DrawRules()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;

        StageDrawBracketLaunchClassifier.TryClassify(stage, [stage]).Should().BeNull();
    }

    [Fact]
    public void ClassifySlots_counts_qual_feed_as_reserved_not_vacant()
    {
        var competitionId = CompetitionId.New();
        var source = Stage.Create(competitionId, new StageName("Groups"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("KO"), SampleRegulations.Standard(), _clock);
        target.AddRound("R1", _clock);
        target.AddSlot("SF1-A");
        target.AddSlot("SF1-B");
        ReplaceStageDrawRules.Execute(target, new DrawRules(DrawMode.Random), _clock);

        source.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(target.Id, "SF1-A"))
            ]),
            _clock);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            target,
            [source, target],
            DrawResolutionKind.Slot);

        facts.EntryPlaceCount.Should().Be(2);
        facts.ReservedByStructuralFeedCount.Should().Be(1);
        facts.VacantForDrawLaunchCount.Should().Be(1);
        facts.BlockingPlaceCount.Should().Be(1);
        facts.OccupiedByEntryCount.Should().Be(0);
    }

    [Fact]
    public void ClassifySlots_entry_on_slot_takes_precedence_over_feed()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var e1 = AddEntry.Execute(competition, "A", _clock);
        var e2 = AddEntry.Execute(competition, "B", _clock);
        var configured = ConfigureStructure.Execute(competition, null, StructureIntent.Cup(2), _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        stage.ReplaceAffectationAuthoring([e1.Id, e2.Id], _clock);
        stage.AssignEntryToSlot(stage.Slots[0].SlotKey, e1.Id);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Slot);

        facts.OccupiedByEntryCount.Should().Be(1);
        facts.ReservedByStructuralFeedCount.Should().Be(0);
        facts.BlockingPlaceCount.Should().Be(1);
    }

    [Fact]
    public void ClassifySlots_multiple_feeds_without_entry_counts_as_invalid()
    {
        var competitionId = CompetitionId.New();
        var sourceA = Stage.Create(competitionId, new StageName("Groups A"), SampleRegulations.Standard(), _clock);
        var sourceB = Stage.Create(competitionId, new StageName("Groups B"), SampleRegulations.Standard(), _clock);
        var target = Stage.Create(competitionId, new StageName("KO"), SampleRegulations.Standard(), _clock);
        target.AddRound("R1", _clock);
        target.AddSlot("SF1-A");
        ReplaceStageDrawRules.Execute(target, new DrawRules(DrawMode.Random), _clock);

        sourceA.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(target.Id, "SF1-A"))
            ]),
            _clock);
        sourceB.ReplaceQualificationRules(
            new QualificationRules(
            [
                new QualificationPath(
                    1,
                    QualificationSource.Overall(),
                    new QualificationSelection(SelectionMode.Position, 1),
                    QualificationDestination.ForSlot(target.Id, "SF1-A"))
            ]),
            _clock);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            target,
            [sourceA, sourceB, target],
            DrawResolutionKind.Slot);

        facts.FeedResolutionInvalidCount.Should().Be(1);
        facts.VacantForDrawLaunchCount.Should().Be(0);
        facts.BlockingPlaceCount.Should().Be(1);
    }

    [Fact]
    public void ClassifyGroups_counts_group_entries_as_occupied()
    {
        var competition = CreateCompetition.Execute("G", _clock);
        for (var i = 0; i < 4; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 2),
            _clock);
        var stage = configured.Stage;
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        stage.ReplaceAffectationAuthoring([.. competition.Entries.Select(e => e.Id)], _clock);

        var facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Group);

        facts.EntryPlaceCount.Should().Be(4);
        facts.BlockingPlaceCount.Should().Be(0);

        var draw = CreateDraw.Execute(stage, DrawResolutionKind.Group, _clock);
        var inputs = DrawInputsFactory.CreateDefault(stage, DrawResolutionKind.Group);
        ConfigureDrawInputs.Execute(stage, draw.Id, inputs);
        GenerateDrawResolution.Execute(
            stage,
            draw.Id,
            _clock,
            groupTargets: [.. stage.Groups.Select(g => g.Id)]);
        PublishDraw.Execute(stage, draw.Id, _clock);
        ApplyDraw.Execute(stage, draw.Id, _clock);

        facts = StageDrawBracketLaunchClassifier.ClassifyForDrawKind(
            stage,
            [stage],
            DrawResolutionKind.Group);
        facts.OccupiedByEntryCount.Should().Be(4);
        facts.BlockingPlaceCount.Should().Be(4);
    }
}
