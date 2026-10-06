// -----------------------------------------------------------------------
// <copyright file="StructurePlaceOccupancyIssuesTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructurePlaceOccupancyIssuesTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_projects_SlotFeedMissing_when_groups_places_are_empty_and_draw_not_awaiting()
    {
        var competition = CreateCompetition.Execute("Groups empty places", _clock);
        for (var i = 0; i < 8; i++)
        {
            competition.AddEntry(TeamId.New(), $"Team {i + 1}", _clock);
        }

        var groups = Stage.Create(
            competition.Id,
            new StageName("Groupes"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(groups.Id, _clock);
        groups.AddGroup("A", _clock);
        groups.AddGroup("B", _clock);
        groups.SetPlacesPerGroup(4);
        groups.AddMatchday(1, _clock);
        groups.ReplaceAffectationAuthoring(
            [.. competition.Entries.Select(entry => entry.Id)],
            _clock);

        var view = StructureViewAssembler.Assemble(competition, [groups]);
        var hub = view.Stages.Single();

        hub.CompositionEntryCount.Should().Be(8);
        hub.CompositionCapacity.Should().Be(8);
        hub.StructureIssues.Should().Contain(StructureViewAssembler.IssueSlotFeedMissing);
    }

    [Fact]
    public void Assemble_silences_schematic_SlotFeedMissing_while_draw_awaiting_apply()
    {
        var competition = CreateCompetition.Execute("Draw awaiting", _clock);
        for (var i = 0; i < 4; i++)
        {
            competition.AddEntry(TeamId.New(), $"Team {i + 1}", _clock);
        }

        var groups = Stage.Create(
            competition.Id,
            new StageName("Groupes"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: true),
            _clock);
        competition.AddStage(groups.Id, _clock);
        groups.AddGroup("A", _clock);
        groups.SetPlacesPerGroup(4);
        groups.AddMatchday(1, _clock);
        groups.ReplaceDrawRules(
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);
        groups.CreateDraw(DrawResolutionKind.Group, _clock);

        var view = StructureViewAssembler.Assemble(competition, [groups]);
        var hub = view.Stages.Single();

        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.InProgress);
        hub.StructureIssues.Should().NotContain(StructureViewAssembler.IssueSlotFeedMissing);
    }
}
