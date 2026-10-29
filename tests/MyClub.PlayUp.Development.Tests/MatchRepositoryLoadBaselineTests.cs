// -----------------------------------------------------------------------
// <copyright file="MatchRepositoryLoadBaselineTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Isolates <see cref="IMatchRepository"/> list-load SQL cost (split-query owned collections).
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "PerformanceBaseline")]
public sealed class MatchRepositoryLoadBaselineTests(
    DevelopmentPostgresFixture fixture,
    ITestOutputHelper output)
{
    [Fact]
    public async Task Baseline_match_list_by_stage_cup_qf_sfAsync()
    {
        var competitionId = await SeedCompetitionAsync("cup-qf-sf");
        var stageIds = await LoadStageIdsAsync(competitionId);
        var qfStageId = stageIds[0];

        var listByStageSql = await MeasureAsync(matches => matches.ListByStageForUpdateAsync(qfStageId));
        var listByStageIdsSql = await MeasureAsync(matches =>
            matches.ListByStageIdsForUpdateAsync(stageIds));
        var listByStageIdsReadOnlySql = await MeasureAsync(matches =>
            matches.ListByStageIdsReadOnlyAsync(stageIds));

        WriteBlock(
            "cup-qf-sf — match list loads (QF played, 4 matches with sheet facts)",
            new Dictionary<string, int>
            {
                ["ListByStageForUpdate(QF)"] = listByStageSql,
                ["ListByStageIdsForUpdate(N=2)"] = listByStageIdsSql,
                ["ListByStageIdsReadOnly(N=2)"] = listByStageIdsReadOnlySql
            });

        listByStageSql.Should().BeGreaterThanOrEqualTo(
            5,
            "split load: principal + four owned collections on one stage");
        listByStageSql.Should().BeLessThanOrEqualTo(
            8,
            "AUD-0012 match-list split: no cartesian single query on QF stage");
        listByStageIdsSql.Should().BeGreaterThanOrEqualTo(listByStageSql);
        listByStageIdsReadOnlySql.Should().BeInRange(
            listByStageIdsSql - 1,
            listByStageIdsSql + 1,
            "read-only tracking should not change SQL shape");
    }

    [Fact]
    public async Task Baseline_match_get_by_ids_for_update_groups_runningAsync()
    {
        var competitionId = await SeedCompetitionAsync("groups:running");
        var stageId = (await LoadStageIdsAsync(competitionId)).Single();
        var matchIds = await LoadMatchIdsForStageAsync(stageId);

        var batchSql = await MeasureAsync(matches => matches.GetByIdsForUpdateAsync(matchIds));

        WriteBlock(
            "groups:running — GetByIdsForUpdate(all stage matches)",
            new Dictionary<string, int>
            {
                ["GetByIdsForUpdate(N=12)"] = batchSql
            });

        batchSql.Should().BeGreaterThanOrEqualTo(
            5,
            "split load issues at least one query per owned collection graph");
        batchSql.Should().BeLessThanOrEqualTo(
            30,
            "twelve matches split-loaded without per-id loops");
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
        return competition!.StageIds;
    }

    private async Task<IReadOnlyList<MatchId>> LoadMatchIdsForStageAsync(StageId stageId)
    {
        using var scope = fixture.Services.CreateScope();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var listed = await matches.ListByStageForUpdateAsync(stageId);
        return listed.Select(match => match.Id).ToArray();
    }

    private async Task<int> MeasureAsync(Func<IMatchRepository, Task> action)
    {
        using var scope = fixture.Services.CreateScope();
        var counter = scope.ServiceProvider.GetRequiredService<SqlCommandCounterInterceptor>();
        counter.Reset();

        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        await action(matches);

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
