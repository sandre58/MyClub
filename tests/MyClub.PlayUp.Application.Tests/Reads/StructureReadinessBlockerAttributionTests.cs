// -----------------------------------------------------------------------
// <copyright file="StructureReadinessBlockerAttributionTests.cs" company="Stéphane ANDRE">
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

public sealed class StructureReadinessBlockerAttributionTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_invalid_mono_cup_emits_one_detail_and_legacy_code()
    {
        var situation = TestCompetition.Create("Mono cup invalid", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H");
        var cup = situation.AddRegulationStyleInvalidKnockoutFinal("Phase finale");

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var cupStageId = cup.Id.Value;

        view.Readiness.Blockers.Should().Equal(StructureViewAssembler.BlockerCupBracketInvalid);
        view.Readiness.BlockerDetails.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(
                new StructureReadinessBlockerDto(
                    StructureViewAssembler.BlockerCupBracketInvalid,
                    cupStageId));
    }

    [Fact]
    public void Assemble_groups_primary_with_invalid_knockout_final_attributes_cup_stage()
    {
        var situation = TestCompetition.Create("Groups KO", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4));

        var groupsStageId = situation.RequirePrimaryStage().Id.Value;
        var knockout = situation.AddRegulationStyleInvalidKnockoutFinal("Phase finale");

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);

        view.Readiness.Blockers.Should().Contain(StructureViewAssembler.BlockerCupBracketInvalid);
        view.Readiness.BlockerDetails
            .Where(d => d.Code == StructureViewAssembler.BlockerCupBracketInvalid)
            .Should().ContainSingle()
            .Which.StageId.Should().Be(knockout.Id.Value);
        view.Readiness.BlockerDetails
            .Where(d => d.Code == StructureViewAssembler.BlockerCupBracketInvalid)
            .Should().NotContain(d => d.StageId == groupsStageId);
    }

    [Fact]
    public void Assemble_two_invalid_cups_emit_two_details_one_legacy_code()
    {
        var situation = TestCompetition.Create("Dual cup invalid", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H");

        var cupA = situation.AddRegulationStyleInvalidKnockoutFinal("Cup A").Id.Value;
        var cupB = situation.AddRegulationStyleInvalidKnockoutFinal("Cup B").Id.Value;

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);

        view.Readiness.Blockers.Should().Equal(StructureViewAssembler.BlockerCupBracketInvalid);
        view.Readiness.BlockerDetails
            .Where(d => d.Code == StructureViewAssembler.BlockerCupBracketInvalid)
            .Select(d => d.StageId)
            .Should().Equal(cupA, cupB);
    }

    [Fact]
    public void BlockerDetails_covers_every_legacy_blocker_code()
    {
        var situation = TestCompetition.Create("Empty", _clock);
        var view = StructureViewAssembler.Assemble(situation.Competition, []);

        view.Readiness.Blockers.Should().NotBeEmpty();
        var detailCodes = StructureViewAssembler.DeriveLegacyBlockerCodes(view.Readiness.BlockerDetails);
        detailCodes.Should().Equal(view.Readiness.Blockers);
        foreach (var code in view.Readiness.Blockers)
        {
            view.Readiness.BlockerDetails.Should().Contain(d => d.Code == code);
        }
    }

    [Fact]
    public void Assemble_invalid_cup_blocks_prepare_transition_on_owner_hub()
    {
        var situation = TestCompetition.Create("Cup prepare gate", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H");
        var cup = situation.AddRegulationStyleInvalidKnockoutFinal("Phase finale");

        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single(s => s.StageId == cup.Id.Value);
        hub.Lifecycle!.Transition.Code.Should().Be(OverviewAssembler.ActionPrepareStage);
        hub.Lifecycle.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureViewAssembler.BlockerCupBracketInvalid);
    }

    [Fact]
    public void DeriveLegacyBlockerCodes_preserves_first_occurrence_order()
    {
        var details = new[]
        {
            new StructureReadinessBlockerDto("A", Guid.NewGuid()),
            new StructureReadinessBlockerDto("B", null),
            new StructureReadinessBlockerDto("A", Guid.NewGuid()),
            new StructureReadinessBlockerDto("C", null)
        };

        StructureViewAssembler.DeriveLegacyBlockerCodes(details).Should().Equal("A", "B", "C");
    }
}
