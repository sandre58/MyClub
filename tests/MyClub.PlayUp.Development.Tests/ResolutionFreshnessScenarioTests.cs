// -----------------------------------------------------------------------
// <copyright file="ResolutionFreshnessScenarioTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class ResolutionFreshnessScenarioTests(DevelopmentPostgresFixture fixture)
{
    [Fact]
    public async Task Cup_qf_sf_resolution_occupants_stay_currentAsync()
    {
        await AssertSeededResolutionIsCurrentAsync("cup-qf-sf");
    }

    [Fact]
    public async Task Groups_to_ko_mid_resolution_occupants_stay_currentAsync()
    {
        await AssertSeededResolutionIsCurrentAsync("groups-to-ko-mid");
    }

    private async Task AssertSeededResolutionIsCurrentAsync(string seed)
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse(seed)]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var summary = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();

        var loaded = new List<Stage>(competition.StageIds.Count);
        var summaries = new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>();
        foreach (var stageId in competition.StageIds)
        {
            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
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

        var view = StructureViewAssembler.Assemble(
            competition,
            loaded,
            matchSummariesByStage: summaries);
        var occupants = view.Stages.SelectMany(stage => stage.ResolutionOccupants ?? []).ToArray();
        occupants.Should().NotBeEmpty();
        occupants.Where(occupant => occupant.ProvenanceKind is
                ResolutionProvenanceKind.Qualification
                or ResolutionProvenanceKind.Progression
                or ResolutionProvenanceKind.Tirage
                or ResolutionProvenanceKind.Directe)
            .Should().OnlyContain(occupant => occupant.State == StructureResolutionFreshness.AJour);
    }
}
