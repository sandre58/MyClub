// -----------------------------------------------------------------------
// <copyright file="CtrlO3MatRecetteIntegrationTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using Xunit;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Read-model gate for CTRL-O3-MAT (docs/design/structure-lifecycle/ctrl-o3-mat-devrunner-recette.md).
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
public sealed class CtrlO3MatRecetteIntegrationTests(DevelopmentPostgresFixture fixture)
{
    [Fact]
    public async Task Ctrl_o3_mat_recette_seeds_four_competitionsAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("ctrl-o3-mat-recette")]);

        using var scope = fixture.Services.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().ListAsync();
        list.Should().HaveCount(7);
        list.Should().OnlyContain(c => c.Name.Value.Contains("CTRL-O3-MAT", StringComparison.Ordinal));
    }

    [Fact]
    public async Task S1_groups_suspended_comp_and_stage_materialize_not_command_enabledAsync()
    {
        var hub = await LoadPrimaryStageHubAsync("CTRL-O3-MAT S1");
        hub.Status.Should().Be(StageStatus.Suspended);
        hub.Materialize.Should().NotBeNull();
        hub.Materialize!.CommandEnabled.Should().BeFalse();
        hub.Materialize.MaterializedCount.Should().Be(3);
        hub.Materialize.DeterminedCount.Should().Be(6);
        hub.Lifecycle!.RevertTransition!.Code.Should().Be(OverviewAssembler.ActionResumeStage);
    }

    [Fact]
    public async Task S2_groups_stage_suspended_comp_running_materialize_not_command_enabledAsync()
    {
        var (competition, hub) = await LoadPrimaryStageHubWithCompetitionAsync("CTRL-O3-MAT S2");
        competition.Status.Should().Be(CompetitionStatus.Running);
        hub.Status.Should().Be(StageStatus.Suspended);
        hub.Materialize.Should().NotBeNull();
        hub.Materialize.CommandEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task S3_champ_suspended_after_start_materialize_not_command_enabledAsync()
    {
        var (competition, hub) = await LoadChampionshipHubAsync("CTRL-O3-MAT S3");
        competition.Status.Should().Be(CompetitionStatus.Running);
        hub.Status.Should().Be(StageStatus.Suspended);
        hub.MatchCount.Should().Be(6);
        hub.Materialize.Should().NotBeNull();
        hub.Materialize!.CommandEnabled.Should().BeFalse();
        hub.Lifecycle!.RevertTransition!.Code.Should().Be(OverviewAssembler.ActionResumeStage);
    }

    [Fact]
    public async Task S4_swiss_suspended_has_no_batch_materialize_projectionAsync()
    {
        var hub = await LoadPrimaryStageHubAsync("CTRL-O3-MAT S4");
        hub.Status.Should().Be(StageStatus.Suspended);
        hub.Materialize.Should().BeNull(
            "Swiss progressive format does not project batch materialize on Structure hub");
        hub.Lifecycle!.RevertTransition!.Code.Should().Be(OverviewAssembler.ActionResumeStage);
    }

    [Fact]
    public async Task S5_cup_suspended_empty_bracket_placement_read_modelAsync()
    {
        var (competition, hub) = await LoadPrimaryStageHubWithCompetitionAsync("CTRL-O3-MAT S5");
        competition.Status.Should().Be(CompetitionStatus.Running);
        hub.Status.Should().Be(StageStatus.Suspended);
        hub.FormatKind.Should().Be(StructureFormatKind.Cup);
        hub.AffectationEntryCount.Should().Be(8);
        hub.CompositionCapacity.Should().Be(8);
        hub.Materialize.Should().NotBeNull();
        hub.Materialize!.CommandEnabled.Should().BeFalse();
        hub.Lifecycle!.RevertTransition!.Code.Should().Be(OverviewAssembler.ActionResumeStage);
    }

    [Fact]
    public async Task S6_cup_suspended_partial_composition_poolAsync()
    {
        var hub = await LoadPrimaryStageHubAsync("CTRL-O3-MAT S6");
        hub.Status.Should().Be(StageStatus.Suspended);
        hub.AffectationEntryCount.Should().Be(8);
        hub.CompositionCapacity.Should().Be(8);
    }

    [Fact]
    public async Task S7_cup_suspended_population_shortfall_on_railAsync()
    {
        var hub = await LoadPrimaryStageHubAsync("CTRL-O3-MAT S7");
        hub.Status.Should().Be(StageStatus.Suspended);
        hub.AffectationEntryCount.Should().Be(5);
        hub.CompositionCapacity.Should().Be(8);
        hub.StructureIssues.Should().Contain("PopulationShortfall");
    }

    private static async Task<IReadOnlyList<MatchSummaryRow>> BuildMatchSummariesAsync(
        IMatchRepository matches,
        StageId stageId)
    {
        var stageMatches = await matches.ListByStageForUpdateAsync(stageId);
        return [.. stageMatches
            .Select(match => new MatchSummaryRow(
                match.Id,
                match.StageId,
                match.Status,
                match.HomeEntryId,
                match.AwayEntryId,
                match.Result,
                match.HasObservedLive))];
    }

    private async Task<StructureStageHubSummaryDto> LoadPrimaryStageHubAsync(string namePrefix)
    {
        var (_, hub) = await LoadPrimaryStageHubWithCompetitionAsync(namePrefix);
        return hub;
    }

    private async Task<(Competition Competition, StructureStageHubSummaryDto Hub)> LoadPrimaryStageHubWithCompetitionAsync(
        string namePrefix)
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("ctrl-o3-mat-recette")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

        var summary = (await competitions.ListAsync())
            .Should()
            .ContainSingle(c => c.Name.Value.StartsWith(namePrefix, StringComparison.Ordinal))
            .Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        var allStages = await stages.GetByIdsForUpdateAsync(competition.StageIds);
        var stage = allStages.OrderBy(s => s.Id.Value).First();

        var matchRows = await BuildMatchSummariesAsync(matches, stage.Id);
        var view = StructureViewAssembler.Assemble(
            competition,
            allStages,
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [stage.Id] = matchRows
            });
        var hub = view.Stages.Single(s => s.StageId == stage.Id.Value);
        return (competition, hub);
    }

    private async Task<(Competition Competition, StructureStageHubSummaryDto Hub)> LoadChampionshipHubAsync(
        string namePrefix)
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("ctrl-o3-mat-recette")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

        var summary = (await competitions.ListAsync())
            .Should()
            .ContainSingle(c => c.Name.Value.StartsWith(namePrefix, StringComparison.Ordinal))
            .Subject;
        var competition = await competitions.GetByIdForUpdateAsync(summary.Id);
        competition.Should().NotBeNull();
        var allStages = await stages.GetByIdsForUpdateAsync(competition.StageIds);
        var champ = allStages.Single(s => s.Name.Value == "Championnat");

        var matchRows = await BuildMatchSummariesAsync(matches, champ.Id);
        var view = StructureViewAssembler.Assemble(
            competition,
            allStages,
            matchSummariesByStage: new Dictionary<StageId, IReadOnlyList<MatchSummaryRow>>
            {
                [champ.Id] = matchRows
            });
        var hub = view.Stages.Single(s => s.StageId == champ.Id.Value);
        return (competition, hub);
    }
}
