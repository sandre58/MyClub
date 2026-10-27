// -----------------------------------------------------------------------
// <copyright file="StageRepositoryLoadBaselineTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Isolates <see cref="IStageRepository"/> SQL cost by <see cref="StageLoadProfile"/> (AUD-0012 / D2).
/// Run with <c>Category=PerformanceBaseline</c> alongside <see cref="ReadPerformanceBaselineTests"/>.
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "PerformanceBaseline")]
public sealed class StageRepositoryLoadBaselineTests(
    DevelopmentPostgresFixture fixture,
    ITestOutputHelper output)
{
    [Fact]
    public async Task Baseline_stage_repository_load_profiles_cup_qf_sfAsync()
    {
        var stageId = await SeedSingleStageAsync("cup-qf-sf");

        var summarySql = await MeasureAsync(stages =>
            stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Summary));
        var structureSql = await MeasureAsync(stages =>
            stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Structure));
        var structureDrawsSlotsSql = await MeasureAsync(async stages =>
            _ = await stages.GetByIdsReadOnlyAsync(
                [stageId],
                new StageReadCapabilities(
                    StageLoadProfile.Structure,
                    IncludeDraws: true,
                    IncludeSlots: true)));
        var fullReadSql = await MeasureAsync(stages =>
            stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Full));
        var fullUpdateSql = await MeasureAsync(stages =>
            stages.GetByIdForUpdateAsync(stageId));

        WriteBlock(
            "cup-qf-sf — single stage (QF stage, 4 entries, played matches)",
            new Dictionary<string, int>
            {
                ["GetByIdReadOnly(Summary)"] = summarySql,
                ["GetByIdReadOnly(Structure) — includes + hydration"] = structureSql,
                ["GetByIdsReadOnly(Structure + Draws + Slots)"] = structureDrawsSlotsSql,
                ["GetByIdReadOnly(Full)"] = fullReadSql,
                ["GetByIdForUpdate(Full)"] = fullUpdateSql,
                ["Structure hydration surplus over Summary"] = structureSql - summarySql
            });

        structureSql.Should().BeGreaterThan(summarySql);
        structureSql.Should().BeLessThanOrEqualTo(
            14,
            "Structure includes plus hydration (unchanged by the stage-overview lot; surplus over Summary is that bundle)");
        structureDrawsSlotsSql.Should().BeGreaterThanOrEqualTo(structureSql);
        structureDrawsSlotsSql.Should().BeLessThan(
            fullReadSql,
            "penalties are the Full collection the stage overview read does not use");
        structureDrawsSlotsSql.Should().BeLessThanOrEqualTo(16, "Structure + Draws + Slots on one cup stage");
        fullUpdateSql.Should().BeInRange(fullReadSql - 2, fullReadSql + 2);
    }

    [Fact]
    public async Task Baseline_stage_repository_multi_id_full_readAsync()
    {
        var competitionId = await SeedCompetitionAsync("cup-qf-sf");
        var stageIds = await LoadStageIdsAsync(competitionId);

        var multiSql = await MeasureAsync(async stages => _ = await stages.GetByIdsReadOnlyAsync(stageIds, StageLoadProfile.Full));

        WriteBlock(
            "cup-qf-sf — GetByIdsReadOnly(Full) for all competition stages",
            new Dictionary<string, int>
            {
                ["GetByIdsReadOnly(Full), N=2"] = multiSql
            });

        multiSql.Should().BeGreaterThan(5);
        multiSql.Should().BeLessThanOrEqualTo(17, "D2-B lot 2 batches hydration across stages (baseline was 18 SQL)");
    }

    [Fact]
    public async Task Baseline_stage_repository_structure_capabilities_attention_shapeAsync()
    {
        var stageId = await SeedSingleStageAsync("cup-qf-sf");
        var capabilities = CompetitionReadBundleSpec.Attention.StageCapabilities;

        var sql = await MeasureAsync(async stages => _ = await stages.GetByIdsReadOnlyAsync([stageId], capabilities));

        WriteBlock(
            "cup-qf-sf — GetByIdsReadOnly(Attention StageCapabilities)",
            new Dictionary<string, int>
            {
                ["Structure + Draws + Slots + Penalties"] = sql
            });

        sql.Should().BeGreaterThan(3);
    }

    private async Task<StageId> SeedSingleStageAsync(string seedSpec)
    {
        var competitionId = await SeedCompetitionAsync(seedSpec);
        var stageIds = await LoadStageIdsAsync(competitionId);
        return stageIds[0];
    }

    private async Task<CompetitionId> SeedCompetitionAsync(string seedSpec)
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse(seedSpec)]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        return (await competitions.ListAsync()).Should().ContainSingle().Subject.Id;
    }

    private async Task<IReadOnlyList<StageId>> LoadStageIdsAsync(CompetitionId competitionId)
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId);
        competition.Should().NotBeNull();
        return competition.StageIds;
    }

    private async Task<int> MeasureAsync(Func<IStageRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        var counter = scope.ServiceProvider.GetRequiredService<SqlCommandCounterInterceptor>();
        counter.Reset();

        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        await action(stages);

        return counter.CommandCount;
    }

    private void WriteBlock(string header, IReadOnlyDictionary<string, int> rows)
    {
        output.WriteLine(string.Empty);
        output.WriteLine(header);
        output.WriteLine(new string('-', 72));
        foreach (var (label, count) in rows)
        {
            output.WriteLine($"{label,-52} {count,4} SQL");
        }
    }
}
