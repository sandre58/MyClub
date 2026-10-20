// -----------------------------------------------------------------------
// <copyright file="StructurePresentedStageDiagnosticsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
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
    public void Assemble_groups_ready_awaiting_prog_inbound_suppresses_match_presented_blockers()
    {
        var situation = TestCompetition.Create("Prog groups presented", _clock)
            .WithTeams(4)
            .WithStructure(StructureIntent.Cup(4));
        var semi = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        for (var i = 0; i < 4; i++)
        {
            semi.ApplyResolvedEntry($"S{i + 1}", entries[i], _clock);
        }

        foreach (var pair in semi.BracketPairs)
        {
            _ = semi.AddFixture(
                semi.Rounds[0].Id,
                _clock,
                pair.SlotAKey,
                pair.SlotBKey,
                pair.PairKey);
        }

        var groups = situation.AddBareStage("Poules");
        groups.AddGroup("A", _clock);
        groups.AddGroup("B", _clock);
        groups.AddMatchday(1, _clock);
        groups.SetPlacesPerGroup(2);
        groups.SetMatchGenerationFormat(MatchGenerationFormat.SingleRoundRobin);
        groups.ReplaceDrawRules(null, _clock);

        var orderedGroups = groups.Groups.OrderBy(group => group.Name, StringComparer.Ordinal).ToArray();
        var semiPairs = semi.BracketPairs.OrderBy(pair => pair.PairKey, StringComparer.Ordinal).ToArray();
        semi.ReplaceProgressionRules(
            new ProgressionRules(
            [
                new ProgressionPath(
                    semiPairs[0].PairKey,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForGroup(groups.Id, orderedGroups[0].Id)),
                new ProgressionPath(
                    semiPairs[1].PairKey,
                    ProgressionOutcome.Winner,
                    ProgressionDestination.ForGroup(groups.Id, orderedGroups[1].Id))
            ]),
            _clock);

        PrepareStage.Execute(groups, situation.Stages, _clock);

        var hub = StructureViewAssembler.Assemble(situation.Competition, situation.Stages)
            .Stages.Single(stage => stage.StageId == groups.Id.Value);

        hub.InboundComposition.Should().NotBeNull();
        hub.InboundComposition!.ConfiguredVolume.Should().BeGreaterThan(0);
        hub.InboundComposition.ResolvedCount.Should().Be(0);
        hub.Materialize!.DeterminedCount.Should().Be(0);
        StructurePresentedStageDiagnostics.IsAwaitingInboundFeedResolution(hub).Should().BeTrue();
        hub.PresentedIssueCodes.Should().NotContain(
            StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
        hub.PresentedIssueCodes.Should().NotContain(
            StructureStageLifecycleAssembler.DisabledNoDeterminedConfrontations);
    }

    [Fact]
    public void Assemble_cup_with_draw_to_launch_suppresses_match_and_pair_production_presented_blockers()
    {
        var situation = TestCompetition.Create("Cup draw pending", _clock)
            .WithTeams(16)
            .WithStructure(StructureIntent.Cup(16));
        var stage = situation.RequirePrimaryStage();
        ReplaceStageDrawRules.Execute(stage, new DrawRules(DrawMode.Random), _clock);
        situation.PreparePrimaryStage();

        var hub = StructureViewAssembler.Assemble(situation.Competition, [stage]).Stages.Single();

        hub.HasDrawRules.Should().BeTrue();
        hub.DrawExecutionBadge.Should().Be(StructureDrawExecutionBadge.ToLaunch);
        hub.PresentedIssueCodes.Should().NotContain(
            StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);
        hub.PresentedIssueCodes.Should().NotContain(
            StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
        hub.MatchProductionPresentation!.PrerequisiteGauge!.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
        hub.MatchProductionPresentation.PrerequisiteGauge.BlocksPhaseStart.Should().BeFalse();
        hub.MatchProductionPresentation.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindInfo);
    }

    [Fact]
    public void Assemble_cup_partial_round_materialization_surfaces_start_and_presented_match_gaps()
    {
        var situation = TestCompetition.Create("Cup partial presented", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.PrimaryStage!;
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry("S1", entries[0], _clock);
        stage.ApplyResolvedEntry("S2", entries[1], _clock);
        situation.PreparePrimaryStage();

        var materialized = MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            ["P1"],
            [],
            _clock);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = StageStartReadinessGuard.ToSummaryRows(materialized.CreatedMatches)
        };

        var hub = StructureViewAssembler.Assemble(
            situation.Competition,
            situation.Stages,
            matchSummariesByStage: summaries).Stages.Single();

        hub.Lifecycle!.Transition.Enabled.Should().BeFalse();
        hub.PresentedIssueCodes.Should().Contain(
            StructureStageLifecycleAssembler.DisabledUnmaterializedConfrontations);
        hub.PresentedIssueCodes.Should().Contain(
            StructureStageLifecycleAssembler.DisabledIncompleteEntryRound);
        hub.MatchProductionPresentation!.PrimaryGauge.MessageKind.Should().Be(
            StructureMatchProductionPresentationAssembler.MessageKindDanger);
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
