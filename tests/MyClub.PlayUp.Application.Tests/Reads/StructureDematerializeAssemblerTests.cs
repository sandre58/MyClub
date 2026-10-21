// -----------------------------------------------------------------------
// <copyright file="StructureDematerializeAssemblerTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Reads;

public sealed class StructureDematerializeAssemblerTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 18, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Championship_materialized_exposes_enabled_full_phase()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        var summaries = materialized.CreatedMatches.Select(Summary).ToArray();

        var dematerialize = StructureDematerializeAssembler.Assemble(situation.Competition, stage, summaries);

        dematerialize.Should().NotBeNull();
        dematerialize.Enabled.Should().BeTrue();
        dematerialize.Scope.Should().Be(DematerializationCapability.ScopeFullPhase);
        dematerialize.DisabledReason.Should().BeNull();
    }

    [Fact]
    public void Championship_without_materialization_omits_dematerialize_facet()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();

        var dematerialize = StructureDematerializeAssembler.Assemble(situation.Competition, stage, []);

        dematerialize.Should().BeNull();
    }

    [Fact]
    public void Finished_match_disables_with_match_lifecycle_reason()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        materialized.CreatedMatches[0].Finish(
            new MatchResult(ResultType.Played, new Score(1, 0)),
            _clock);
        var summaries = materialized.CreatedMatches.Select(Summary).ToArray();

        var dematerialize = StructureDematerializeAssembler.Assemble(situation.Competition, stage, summaries);

        dematerialize.Should().NotBeNull();
        dematerialize.Enabled.Should().BeFalse();
        dematerialize.DisabledReason.Should().Be(DematerializeDisabledReasons.MatchLifecycleStarted);
    }

    [Fact]
    public void Cup_partial_materialization_still_exposes_full_phase_scope()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo", "Charlie", "Delta")
            .WithStructure(StructureIntent.Cup(4));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        stage.Prepare(_clock);
        var materialized = MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock);
        var summaries = materialized.CreatedMatches.Select(Summary).ToArray();

        var dematerialize = StructureDematerializeAssembler.Assemble(situation.Competition, stage, summaries);

        dematerialize.Should().NotBeNull();
        dematerialize.Scope.Should().Be(DematerializationCapability.ScopeFullPhase);
        dematerialize.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Structure_view_includes_dematerialize_when_materialized()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.PreparePrimaryStageForMaterialization();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        var summariesByStage = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
        {
            [stage.Id] = [.. materialized.CreatedMatches.Select(Summary)]
        };

        var view = StructureViewAssembler.Assemble(
            situation.Competition,
            [stage],
            matchSummariesByStage: summariesByStage);

        view.Stages[0].Dematerialize.Should().NotBeNull();
        view.Stages[0].Dematerialize!.Enabled.Should().BeTrue();
    }

    private static MatchSummaryRow Summary(Match match) =>
        new(
            match.Id,
            match.StageId,
            match.Status,
            match.HomeEntryId,
            match.AwayEntryId,
            match.Result,
            match.HasObservedLive);
}
