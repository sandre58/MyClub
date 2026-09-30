// -----------------------------------------------------------------------
// <copyright file="ReadPerformanceBaselineTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Templates;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Establishes SQL command baselines for representative read scenarios.
/// Each endpoint is measured in an isolated scope (one HTTP request ≈ one scope).
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "PerformanceBaseline")]
public sealed class ReadPerformanceBaselineTests(
    DevelopmentPostgresFixture fixture,
    ITestOutputHelper output)
{
    [Fact]
    public async Task Baseline_cup_qf_sf_multi_stage_readsAsync()
    {
        var competitionId = await SeedAsync("cup-qf-sf");
        var stageIds = await LoadStageIdsAsync(competitionId);

        var overviewSql = await MeasureAsync(executor => executor.GetOverviewViewAsync(competitionId));
        var structureSql = await MeasureAsync(executor => executor.GetStructureViewAsync(competitionId));
        var consultationSql = await MeasureAsync(executor => executor.GetConsultationAsync(competitionId));
        var attentionSql = await MeasureAsync(executor => executor.GetNeedsAttentionAsync(competitionId));
        var detailSql = await MeasureAsync(executor => executor.GetCompetitionDetailAsync(competitionId));
        var matchHubSql = await MeasureAsync(executor => executor.GetMatchHubViewAsync(competitionId));

        var matchHubStageSql = new List<(Guid StageId, int Sql)>(stageIds.Count);
        var matchHubFanOutTotalSql = detailSql;
        foreach (var stageId in stageIds)
        {
            var stageSql = await MeasureAsync(executor => executor.ListMatchesByStageAsync(stageId));
            matchHubStageSql.Add((stageId.Value, stageSql));
            matchHubFanOutTotalSql += stageSql;
        }

        var overviewPageSql = overviewSql + structureSql;
        var classementsPageSql = consultationSql + structureSql;
        var shellChromeSql = attentionSql + detailSql;

        WriteScenario(
            """
            Scenario: cup-qf-sf (2 stages, QF played, SF slots filled)
            Competition shape: 2 stages, 8 entries, matches on QF stage
            """,
            new Dictionary<string, int>
            {
                ["GET /overview (GetOverviewViewAsync)"] = overviewSql,
                ["GET /structure (GetStructureViewAsync)"] = structureSql,
                ["GET /consultation (GetConsultationAsync)"] = consultationSql,
                ["GET /attention (GetNeedsAttentionAsync)"] = attentionSql,
                ["GET /competitions/{id} (GetCompetitionDetailAsync)"] = detailSql,
                ["GET /matches-hub (GetMatchHubViewAsync)"] = matchHubSql,
                ["Match Hub fan-out — GET /competitions/{id}"] = detailSql,
                ["Match Hub fan-out — sum GET /stages/{id}/matches"] = matchHubStageSql.Sum(row => row.Sql),
                ["Match Hub fan-out — total (1 + N HTTP requests)"] = matchHubFanOutTotalSql,
                ["Overview page (overview + structure)"] = overviewPageSql,
                ["Classements page (consultation + structure)"] = classementsPageSql,
                ["Shell chrome (attention + detail)"] = shellChromeSql
            },
            [.. matchHubStageSql.Select(row => $"  stage {row.StageId}: {row.Sql} SQL")]);

        overviewSql.Should().BeGreaterThan(8, "baseline sanity — overview still issues multiple SQL commands");
        detailSql.Should().BeLessThan(10, "GetCompetitionDetail should use projection, not full stage graphs");
        matchHubSql.Should().BeLessThan(matchHubFanOutTotalSql, "unified Match Hub should beat 1+N fan-out");
        attentionSql.Should().BeLessThanOrEqualTo(overviewSql, "attention bundle must not exceed overview load");
        attentionSql.Should().BeLessThanOrEqualTo(21, "attention path should stay near shell overview cost (includes CompositionEntries)");
        consultationSql.Should().BeLessThanOrEqualTo(overviewSql, "consultation bundle must not exceed overview load");
        structureSql.Should().BeLessThan(overviewSql, "structure bundle should beat full overview load");
    }

    [Fact]
    public async Task Baseline_groups_running_single_stage_many_matchesAsync()
    {
        var competitionId = await SeedAsync("groups:running");
        var stageIds = await LoadStageIdsAsync(competitionId);
        stageIds.Should().ContainSingle();

        var overviewSql = await MeasureAsync(executor => executor.GetOverviewViewAsync(competitionId));
        var detailSql = await MeasureAsync(executor => executor.GetCompetitionDetailAsync(competitionId));
        var matchesSql = await MeasureAsync(executor => executor.ListMatchesByStageAsync(stageIds[0]));
        var matchHubSql = await MeasureAsync(executor => executor.GetMatchHubViewAsync(competitionId));

        WriteScenario(
            """
            Scenario: groups:running (1 stage, double round-robin, partial results)
            Competition shape: 1 stage, 4 entries, 12 matches (6 finished / 6 scheduled)
            """,
            new Dictionary<string, int>
            {
                ["GET /overview"] = overviewSql,
                ["GET /competitions/{id}"] = detailSql,
                ["GET /stages/{id}/matches"] = matchesSql,
                ["GET /matches-hub"] = matchHubSql,
                ["Match Hub fan-out (detail + 1 stage matches)"] = detailSql + matchesSql
            });

        matchesSql.Should().BeGreaterThan(3, "list endpoint loads stage + competition + matches");
    }

    [Fact]
    public async Task Baseline_champions_league_running_heavy_single_stageAsync()
    {
        var templates = fixture.Services.GetRequiredService<TemplateRunner>();
        await templates.ResetAndRunAsync([SeedSpec.Parse("champions-league:running")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var competitionId = (await competitions.ListAsync()).Should().ContainSingle().Subject.Id;

        var overviewSql = await MeasureAsync(executor => executor.GetOverviewViewAsync(competitionId));
        var stageIds = await LoadStageIdsAsync(competitionId);
        var matchesSql = await MeasureAsync(executor => executor.ListMatchesByStageAsync(stageIds[0]));

        WriteScenario(
            """
            Scenario: champions-league:running (1 stage, 32 teams, finished matches with sheet facts)
            Competition shape: 1 stage, 32 entries, many matches with goals/cards
            """,
            new Dictionary<string, int>
            {
                ["GET /overview"] = overviewSql,
                ["GET /stages/{id}/matches"] = matchesSql
            });

        matchesSql.Should().BeGreaterThan(10, "heavy match list should issue many SQL commands today");
    }

    private async Task<CompetitionId> SeedAsync(string seedSpec)
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

    private async Task<int> MeasureAsync(Func<UseCaseExecutor, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        var counter = scope.ServiceProvider.GetRequiredService<SqlCommandCounterInterceptor>();
        counter.Reset();

        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        await action(executor);

        return counter.CommandCount;
    }

    private void WriteScenario(string header, IReadOnlyDictionary<string, int> rows, params string[] details)
    {
        output.WriteLine(string.Empty);
        output.WriteLine(header.Trim());
        output.WriteLine(new string('-', 72));
        foreach (var (label, count) in rows)
        {
            output.WriteLine($"{label,-52} {count,4} SQL");
        }

        if (details.Length > 0)
        {
            output.WriteLine(string.Empty);
            output.WriteLine("Breakdown:");
            foreach (var line in details)
            {
                output.WriteLine(line);
            }
        }

        output.WriteLine(string.Empty);
        output.WriteLine($"TOTAL (sum of listed rows, may double-count shared calls): {rows.Values.Sum()} SQL");
    }
}
