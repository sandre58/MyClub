// -----------------------------------------------------------------------
// <copyright file="StructurePresentedStageDiagnosticsTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructurePresentedStageDiagnosticsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Assemble_championship_ready_without_matches_exposes_Unmaterialized_on_hub()
    {
        var situation = TestCompetition.Create("Ch contract", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);
        var hub = view.Stages.Single();

        hub.Status.Should().Be(StageStatus.Ready);
        hub.Materialize.Should().NotBeNull();
        hub.Materialize!.DeterminedCount.Should().Be(6);
        hub.Materialize.MaterializedCount.Should().Be(0);
        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.Lifecycle.Transition.DisabledReasonCode.Should()
            .Be(StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);

        hub.PresentedIssueCodes.Should().Contain(
            StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
    }

    [Fact]
    public void CollectPresentedIssueCodes_matches_hub_field_after_assemble()
    {
        var situation = TestCompetition.Create("Parity", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship())
            .PreparePrimaryStage();
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);

        foreach (var hub in view.Stages)
        {
            var computed = StructurePresentedStageDiagnostics.CollectPresentedIssueCodes(view, hub);
            hub.PresentedIssueCodes.Should().Equal(computed);
        }
    }

    [Fact]
    public void Attention_collector_emits_same_stage_sources_as_presentedIssueCodes()
    {
        var situation = TestCompetition.Create("KO attrib", _clock)
            .WithTeams("A", "B", "C", "D", "E", "F", "G", "H")
            .WithStructure(StructureIntent.Groups(2, 4));
        situation.AddRegulationStyleInvalidKnockoutFinal("Phase finale");
        var view = StructureViewAssembler.Assemble(situation.Competition, situation.Stages);

        var items = new List<NeedsAttentionItemDto>();
        StructurePresentedAttentionCollector.Collect(view, items);

        foreach (var hub in view.Stages.Where(s => s.Status != StageStatus.Completed))
        {
            var expected = hub.PresentedIssueCodes ?? [];
            foreach (var code in expected)
            {
                if (code is StructureViewAssembler.BlockerMissingStage
                    or StructureViewAssembler.BlockerMissingStructure
                    or StructureViewAssembler.BlockerStructureGraphInvalid)
                {
                    continue;
                }

                items.Should().Contain(
                    item => item.Source == code
                        && item.TargetType == "Stage"
                        && item.TargetId == hub.StageId.ToString(),
                    $"stage {hub.Name} should surface {code} on /attention");
            }
        }
    }

    [Fact]
    public void MissingPotRules_comes_from_readiness_blockerDetails_not_parallel_rule()
    {
        var competition = CreateCompetition.Execute("Pots", _clock);
        for (var i = 0; i < 8; i++)
        {
            competition.AddEntry(TeamId.New(), $"T{i}", _clock);
        }

        var configured = ConfigureStructure.Execute(
            competition,
            null,
            StructureIntent.Groups(2, 4),
            _clock);
        configured.Stage.ReplaceDrawRules(
            new DrawRules(DrawMode.Random, potRules: null),
            _clock);

        var view = StructureViewAssembler.Assemble(competition, [configured.Stage]);
        var hub = view.Stages.Single();

        view.Readiness.BlockerDetails.Should().Contain(d =>
            d.Code == StructureViewAssembler.BlockerMissingPotRules
            && d.StageId == hub.StageId);
        hub.PresentedIssueCodes.Should().Contain(StructureViewAssembler.BlockerMissingPotRules);
    }
}
