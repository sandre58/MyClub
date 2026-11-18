// -----------------------------------------------------------------------
// <copyright file="StructureCompositionEntryPotsTests.cs" company="Stéphane ANDRE">
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

public sealed class StructureCompositionEntryPotsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 11, 11, 9, 30, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_fills_composition_entry_pots_for_groups_in_composition_order()
    {
        var situation = TestCompetition.Create("Groups pots", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4));
        var stage = situation.PrimaryStage!;
        var ids = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ReplaceAffectationAuthoring(ids, _clock);
        ReplaceStageDrawRules.Execute(
            stage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(4)),
            _clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages).Stages.Single();

        hub.CompositionEntryPots.Should().NotBeNull();
        hub.CompositionEntryPots!.Select(row => row.EntryId).Should().Equal(ids.Select(id => id.Value));
        hub.CompositionEntryPots.Select(row => row.Pot).Should().Equal(1, 1, 2, 2, 3, 3, 4, 4);
    }

    [Fact]
    public void Assemble_omits_composition_entry_pots_for_cup_groups_without_pots_and_an_indivisible_count()
    {
        var cup = TestCompetition.Create("Cup pots", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var cupStage = cup.PrimaryStage!;
        cupStage.ReplaceAffectationAuthoring(
            cup.Competition.Entries.Select(entry => entry.Id).ToArray(),
            _clock);
        ReplaceStageDrawRules.Execute(
            cupStage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(2)),
            _clock);

        var plain = TestCompetition.Create("Groups no pots", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Groups(2, 2));
        var plainStage = plain.PrimaryStage!;
        plainStage.ReplaceAffectationAuthoring(
            plain.Competition.Entries.Select(entry => entry.Id).ToArray(),
            _clock);

        var indivisible = TestCompetition.Create("Groups indivisible", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4));
        var indivisibleStage = indivisible.PrimaryStage!;
        indivisibleStage.ReplaceAffectationAuthoring(
            indivisible.Competition.Entries.Select(entry => entry.Id).ToArray(),
            _clock);
        ReplaceStageDrawRules.Execute(
            indivisibleStage,
            new DrawRules(DrawMode.Random, potRules: new PotRules(3)),
            _clock);

        StructureViewAssembler.Assemble(cup.Competition, cup.Stages).Stages.Single()
            .CompositionEntryPots.Should().BeNull();
        StructureViewAssembler.Assemble(plain.Competition, plain.Stages).Stages.Single()
            .CompositionEntryPots.Should().BeNull();
        StructureViewAssembler.Assemble(indivisible.Competition, indivisible.Stages).Stages.Single()
            .CompositionEntryPots.Should().BeNull();
    }
}
