// -----------------------------------------------------------------------
// <copyright file="StructurePresentedAttentionCollectorCupBracketTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructurePresentedAttentionCollectorCupBracketTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Collect_emits_one_attention_row_per_cup_bracket_detail_with_stage_id()
    {
        var situation = TestCompetition.Create("KO attrib", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4));

        var knockout = situation.AddRegulationStyleInvalidKnockoutFinal("Phase finale");
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var items = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(view, items);

        var cupItems = items
            .Where(i => i.Source == StructureViewAssembler.BlockerCupBracketInvalid)
            .ToList();
        cupItems.Should().ContainSingle();
        cupItems[0].Params.Should().ContainKey("stageId");
        cupItems[0].Params!["stageId"].Should().Be(knockout.Id.Value.ToString());
        cupItems[0].TargetType.Should().Be("Stage");
        cupItems[0].TargetId.Should().Be(knockout.Id.Value.ToString());
    }

    [Fact]
    public void Collect_emits_two_rows_for_two_invalid_cups()
    {
        var situation = TestCompetition.Create("Dual KO", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H");

        var cupA = situation.AddRegulationStyleInvalidKnockoutFinal("Cup A");
        var cupB = situation.AddRegulationStyleInvalidKnockoutFinal("Cup B");
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var items = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(view, items);

        items.Where(i => i.Source == StructureViewAssembler.BlockerCupBracketInvalid)
            .Select(i => i.Params!["stageId"])
            .Should().Equal(cupA.Id.Value.ToString(), cupB.Id.Value.ToString());
    }
}
