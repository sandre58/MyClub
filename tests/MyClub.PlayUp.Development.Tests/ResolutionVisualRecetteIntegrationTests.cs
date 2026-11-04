// -----------------------------------------------------------------------
// <copyright file="ResolutionVisualRecetteIntegrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class ResolutionVisualRecetteIntegrationTests(DevelopmentPostgresFixture fixture)
{
    private static StructureResolutionOccupantDto Occupant(
        StructureViewDto view,
        string stageName,
        string slotKey) =>
        view.Stages.Single(stage => stage.Name == stageName)
            .ResolutionOccupants!
            .Single(occupant => occupant.SlotKey == slotKey);

    private static IEnumerable<StructureResolutionOccupantDto> DrawOccupants(StructureViewDto view)
    {
        var cup = view.Stages.Single(stage => stage.Name == "Cup");
        return cup.ResolutionOccupants!.Where(occupant =>
            occupant.ProvenanceKind == ResolutionProvenanceKind.Tirage);
    }

    private static async Task<StructureViewDto> AssembleStructureAsync(IServiceScope scope, CompetitionId competitionId)
    {
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var competition = (await competitions.GetByIdForUpdateAsync(competitionId))!;
        var loaded = new List<Stage>(competition.StageIds.Count);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();
        foreach (var stageId in competition.StageIds)
        {
            var stage = (await stages.GetByIdForUpdateAsync(stageId))!;
            loaded.Add(stage);
            var stageMatches = await matches.ListByStageForUpdateAsync(stageId);
            summaries[stageId] = stageMatches
                .Select(match => new MatchSummaryRow(
                    match.Id,
                    match.StageId,
                    match.Status,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    match.Result,
                    match.HasObservedLive))
                .ToArray();
        }

        return StructureViewAssembler.Assemble(competition, loaded, matchSummariesByStage: summaries);
    }

    [Fact]
    public async Task Resolution_visual_recette_seeds_ten_competitionsAsync()
    {
        await RunScenarioAsync();
        using var scope = fixture.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().ListAsync();
        list.Should().HaveCount(10);
        list.Select(c => c.Name.Value).Should().BeEquivalentTo(
        [
            ResolutionVisualRecetteNames.QualRuleSlot,
            ResolutionVisualRecetteNames.UpstreamScoreSlot,
            ResolutionVisualRecetteNames.AmontPerimeCascade,
            ResolutionVisualRecetteNames.IndeterminateQual,
            ResolutionVisualRecetteNames.NonVerifieeSlot,
            ResolutionVisualRecetteNames.QualRulePopulation,
            ResolutionVisualRecetteNames.QualRuleGroup,
            ResolutionVisualRecetteNames.StaleDrawActive,
            ResolutionVisualRecetteNames.StaleDrawBlocked,
            ResolutionVisualRecetteNames.WithdrawnOccupant,
        ]);
    }

    private async Task AssertCupSchematicAsync(
        string competitionName,
        string stageName,
        params string[] slotKeys)
    {
        await RunScenarioAsync();
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single(c => c.Name.Value == competitionName);
        var competition = (await competitions.GetByIdForUpdateAsync(summary.Id))!;
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var loaded = new List<Stage>(competition.StageIds.Count);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();
        foreach (var stageId in competition.StageIds)
        {
            var loadedStage = (await stages.GetByIdForUpdateAsync(stageId))!;
            loaded.Add(loadedStage);
            var stageMatches = await matches.ListByStageForUpdateAsync(stageId);
            summaries[stageId] = stageMatches
                .Select(match => new MatchSummaryRow(
                    match.Id,
                    match.StageId,
                    match.Status,
                    match.HomeEntryId,
                    match.AwayEntryId,
                    match.Result,
                    match.HasObservedLive))
                .ToArray();
        }

        var stage = loaded.Single(s => s.Name.Value == stageName);
        var schematic = StageSchematicAssembler.Assemble(
            stage,
            competition,
            loaded,
            matchSummariesByStage: summaries);
        schematic.FormatKind.Should().Be(StructureFormatKind.Cup);
        schematic.Cases.Should().NotBeEmpty();
        foreach (var slotKey in slotKeys)
        {
            schematic.Cases.Should().Contain(place =>
                place.FormPosition.Kind == StageSchematicAssembler.FormKindCupSlot
                && place.FormPosition.SlotKey == slotKey);
        }
    }

    [Fact]
    public async Task Visual_recette_cup_schematics_are_structurally_completeAsync()
    {
        await AssertCupSchematicAsync(ResolutionVisualRecetteNames.QualRuleSlot, "Terminal", "Champ");
        await AssertCupSchematicAsync(ResolutionVisualRecetteNames.IndeterminateQual, "Terminal", "Place");
        await AssertCupSchematicAsync(ResolutionVisualRecetteNames.NonVerifieeSlot, "Terminal", "NV");
        await AssertCupSchematicAsync(ResolutionVisualRecetteNames.AmontPerimeCascade, "Phase C", "Finale");
        await AssertCupSchematicAsync(ResolutionVisualRecetteNames.QualRulePopulation, "Knockout", "S1", "S2");
        await AssertCupSchematicAsync(ResolutionVisualRecetteNames.StaleDrawActive, "Cup", "S1", "S2");
        await AssertCupSchematicAsync(ResolutionVisualRecetteNames.StaleDrawBlocked, "Cup", "S1", "S2");
    }

    [Fact]
    public async Task Qual_rule_slot_is_stale_on_terminalAsync()
    {
        var view = await LoadStructureForCompetitionAsync(ResolutionVisualRecetteNames.QualRuleSlot);
        Occupant(view, "Terminal", "Champ")
            .Should().BeEquivalentTo(new
            {
                State = StructureResolutionFreshness.Perime,
                StalenessCause = StructureResolutionStalenessCause.RegleQualification,
            });
    }

    [Fact]
    public async Task Upstream_score_slot_is_staleAsync()
    {
        var view = await LoadStructureForCompetitionAsync(ResolutionVisualRecetteNames.UpstreamScoreSlot);
        Occupant(view, "Terminal", "Champ")
            .Should().BeEquivalentTo(new
            {
                State = StructureResolutionFreshness.Perime,
                StalenessCause = StructureResolutionStalenessCause.ResultatAmont,
            });
    }

    [Fact]
    public async Task Cascade_marks_amont_perime_on_phase_cAsync()
    {
        var view = await LoadStructureForCompetitionAsync(ResolutionVisualRecetteNames.AmontPerimeCascade);
        Occupant(view, "Phase B", "QF-A").State.Should().Be(StructureResolutionFreshness.Perime);
        Occupant(view, "Phase C", "Finale")
            .Should().BeEquivalentTo(new
            {
                State = StructureResolutionFreshness.Perime,
                StalenessCause = StructureResolutionStalenessCause.AmontPerime,
            });
    }

    [Fact]
    public async Task Indeterminate_qual_on_terminalAsync()
    {
        var view = await LoadStructureForCompetitionAsync(ResolutionVisualRecetteNames.IndeterminateQual);
        Occupant(view, "Terminal", "Place").State.Should().Be(StructureResolutionFreshness.Indetermine);
    }

    [Fact]
    public async Task Non_verifiee_slotAsync()
    {
        var view = await LoadStructureForCompetitionAsync(ResolutionVisualRecetteNames.NonVerifieeSlot);
        var occupant = Occupant(view, "Terminal", "NV");
        occupant.State.Should().Be(StructureResolutionFreshness.NonVerifiee);
        occupant.ProvenanceKind.Should().Be(ResolutionProvenanceKind.NonVerifiee);
    }

    [Fact]
    public async Task Population_qualifiers_stale_on_downstreamAsync()
    {
        var view = await LoadStructureForCompetitionAsync(ResolutionVisualRecetteNames.QualRulePopulation);
        var downstream = view.Stages.Single(stage => stage.Name == "Knockout");
        var qualOccupants = (downstream.ResolutionOccupants ?? [])
            .Where(occupant => occupant.ProvenanceKind == ResolutionProvenanceKind.Qualification)
            .ToArray();
        qualOccupants.Should().NotBeEmpty();
        qualOccupants.Should().OnlyContain(occupant =>
            occupant.State == StructureResolutionFreshness.Perime
            && occupant.StalenessCause == StructureResolutionStalenessCause.RegleQualification);
    }

    [Fact]
    public async Task Group_seat_qualifier_staleAsync()
    {
        var view = await LoadStructureForCompetitionAsync(ResolutionVisualRecetteNames.QualRuleGroup);
        var dest = view.Stages.Single(stage => stage.Name == "Poules destination");
        var groupOccupants = (dest.ResolutionOccupants ?? [])
            .Where(occupant => occupant.Target == StructureResolutionOccupantTarget.Group)
            .ToArray();
        groupOccupants.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                State = StructureResolutionFreshness.Perime,
                StalenessCause = StructureResolutionStalenessCause.RegleQualification,
            });
    }

    [Fact]
    public async Task Stale_draw_active_redo_and_release_availableAsync()
    {
        var (competition, cup, view) = await LoadCupCaseAsync(ResolutionVisualRecetteNames.StaleDrawActive);
        DrawOccupants(view).Should().OnlyContain(occupant =>
            occupant.State == StructureResolutionFreshness.Perime);
        var overview = StageOverviewAssembler.Assemble(cup, competition, [cup]);
        var draw = overview.Draws.Single(d => d.IsApplied);
        draw.Actions!.Redo.State.Should().Be(StageDrawActionState.Available);
        draw.Actions.Release.ReleasableCount.Should().BeGreaterThan(0);
        draw.Actions.Release.State.Should().Be(StageDrawActionState.Blocked);
        draw.Actions.Release.BlockCode.Should().Be(ApplicationErrorCodes.DrawReleaseRequiresCancelled);
        draw.Status.Should().Be(DrawStatus.Published);
        draw.IsApplied.Should().BeTrue();
        cup.Status.Should().Be(StageStatus.Ready);
    }

    [Fact]
    public async Task Stale_draw_blocked_redo_and_release_blockedAsync()
    {
        var (competition, cup, view) = await LoadCupCaseAsync(ResolutionVisualRecetteNames.StaleDrawBlocked);
        DrawOccupants(view).Should().OnlyContain(occupant =>
            occupant.State == StructureResolutionFreshness.Perime);
        var overview = StageOverviewAssembler.Assemble(cup, competition, [cup]);
        var draw = overview.Draws.Single(d => d.IsApplied);
        draw.Actions!.Redo.State.Should().Be(StageDrawActionState.Blocked);
        draw.Actions.Redo.BlockCode.Should().Be(StageErrorCodes.MaterializedOpponentConflict);
        draw.Actions.Release.ReleasableCount.Should().BeGreaterThan(0);
        draw.Actions.Release.State.Should().Be(StageDrawActionState.Blocked);
        draw.Actions.Release.BlockCode.Should().Be(ApplicationErrorCodes.DrawReleaseRequiresCancelled);
        draw.Status.Should().Be(DrawStatus.Published);
        draw.IsApplied.Should().BeTrue();
        cup.Status.Should().Be(StageStatus.Ready);
    }

    private async Task RunScenarioAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse(ResolutionVisualRecetteNames.ScenarioId)]);
    }

    private async Task<StructureViewDto> LoadStructureForCompetitionAsync(string competitionName)
    {
        await RunScenarioAsync();
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single(c => c.Name.Value == competitionName);
        return await AssembleStructureAsync(scope, summary.Id);
    }

    private async Task<(Competition Competition, Stage Cup, StructureViewDto View)> LoadCupCaseAsync(
        string competitionName)
    {
        await RunScenarioAsync();
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var summary = (await competitions.ListAsync()).Single(c => c.Name.Value == competitionName);
        var competition = (await competitions.GetByIdForUpdateAsync(summary.Id))!;
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var cup = (await stages.GetByIdForUpdateAsync(competition.StageIds[0]))!;
        var view = await AssembleStructureAsync(scope, summary.Id);
        return (competition, cup, view);
    }
}
