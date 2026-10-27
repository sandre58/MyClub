// -----------------------------------------------------------------------
// <copyright file="StructurePlacesContractTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

/// <summary>
/// Places N / compositionCapacity hub contract.
/// </summary>
public sealed class StructurePlacesContractTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 13, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_root_championship_exposes_active_roster_as_compositionCapacity()
    {
        var situation = TestCompetition.Create("Champ root", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship(stageName: "League"));

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.IsRootComposition.Should().BeTrue();
        hub.CompositionCapacity.Should().Be(4);
    }

    [Fact]
    public void Assemble_root_swiss_exposes_active_roster_as_compositionCapacity()
    {
        var situation = TestCompetition.Create("Swiss root", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Swiss(3, stageName: "Swiss"));

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.IsRootComposition.Should().BeTrue();
        hub.CompositionCapacity.Should().Be(8);
        hub.SwissRoundCount.Should().Be(3);
    }

    [Fact]
    public void Assemble_championship_fed_by_qual_is_not_root_but_keeps_compositionCapacity_for_own_fiche()
    {
        var situation = TestCompetition.Create("Groups to champ", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4, "Poules"));
        var groups = situation.PrimaryStage!;
        var orderedGroups = groups.Groups.OrderBy(g => g.Name, StringComparer.Ordinal).ToArray();
        var entries = situation.Competition.Entries.ToList();
        for (var i = 0; i < entries.Count; i++)
        {
            groups.AssignEntryToGroup(orderedGroups[i / 4].Id, entries[i].Id);
        }

        var league = situation.AddBareStage("League");
        league.AddMatchday(1, _clock);

        ReplaceStageQualificationRules.Execute(
            groups,
            [
                new QualificationIntentSpec(
                    Guid.NewGuid(),
                    1,
                    QualificationIntentSourceKind.EachGroup,
                    1,
                    2,
                    league.Id.Value)
            ],
            _clock);

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var champHub = view.Stages.Single(h => h.StageId == league.Id.Value);

        champHub.IsRootComposition.Should().BeFalse();
        champHub.CompositionCapacity.Should().Be(4);

        var schematic = StageSchematicAssembler.Assemble(
            league,
            situation.Competition,
            situation.Stages);
        schematic.Cases.Should().HaveCount(champHub.CompositionCapacity!.Value);
        schematic.Cases.Should().NotHaveCount(situation.Competition.Entries.Count);
    }
}
