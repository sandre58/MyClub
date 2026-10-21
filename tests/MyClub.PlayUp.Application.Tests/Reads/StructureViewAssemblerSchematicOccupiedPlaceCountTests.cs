// -----------------------------------------------------------------------
// <copyright file="StructureViewAssemblerSchematicOccupiedPlaceCountTests.cs" company="Stéphane ANDRE">
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

public sealed class StructureViewAssemblerSchematicOccupiedPlaceCountTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Hub_projects_zero_occupied_when_groups_places_are_empty()
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

        var hub = StructureViewAssembler.Assemble(competition, [groups]).Stages.Single();

        hub.CompositionCapacity.Should().Be(8);
        hub.SchematicOccupiedPlaceCount.Should().Be(0);
    }

    [Fact]
    public void Hub_schematic_occupied_is_null_when_places_n_unknown()
    {
        var competition = CreateCompetition.Execute("No form", _clock);
        var stage = Stage.Create(
            competition.Id,
            new StageName("Orphan"),
            StageRegulation.MaterializeFrom(competition.Regulation, isClassifyingPhase: false),
            _clock);
        competition.AddStage(stage.Id, _clock);

        var hub = StructureViewAssembler.Assemble(competition, [stage]).Stages.Single();

        hub.SchematicOccupiedPlaceCount.Should().BeNull();
    }
}
