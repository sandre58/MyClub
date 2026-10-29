// -----------------------------------------------------------------------
// <copyright file="WritePerformanceBaselineTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// Command-path SQL and latency baselines (AUD-0012 write guards).
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "PerformanceBaseline")]
public sealed class WritePerformanceBaselineTests(
    DevelopmentPostgresFixture fixture,
    ITestOutputHelper output)
{
    [Fact]
    public async Task Baseline_replace_qualification_rules_refused_on_running_qf_seedAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("cup-qf-sf")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var competition = (await competitions.ListAsync()).Should().ContainSingle().Subject;
        var qfStageId = await FindRunningStageIdAsync(competition.StageIds, stages);
        qfStageId.Should().NotBeNull();
        var qfStage = await stages.GetByIdForUpdateAsync(qfStageId!.Value);
        qfStage.Should().NotBeNull();
        qfStage!.Status.Should().Be(StageStatus.Running);

        var counter = scope.ServiceProvider.GetRequiredService<SqlCommandCounterInterceptor>();
        counter.Reset();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var stopwatch = Stopwatch.StartNew();

        var act = async () => await executor.ReplaceStageQualificationRulesAsync(
            qfStageId!.Value,
            paths: null);

        await act.Should().ThrowAsync<ApplicationFailureException>()
            .Where(ex => ex.Code == ApplicationErrorCodes.StructureNotMutable);

        stopwatch.Stop();

        WriteBlock(
            "cup-qf-sf — ReplaceStageQualificationRulesAsync refused (Running QF)",
            new Dictionary<string, object>
            {
                ["SQL commands"] = counter.CommandCount,
                ["Elapsed ms"] = stopwatch.ElapsedMilliseconds
            });

        counter.CommandCount.Should().BeLessThanOrEqualTo(
            20,
            "S2A early status guard must not load QF match graphs before refusal");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(
            150,
            "refusal on Running upstream must stay under product latency budget on seed");
    }

    private static async Task<StageId?> FindRunningStageIdAsync(
        IReadOnlyList<StageId> stageIds,
        IStageRepository stages)
    {
        foreach (var stageId in stageIds)
        {
            var stage = await stages.GetByIdForUpdateAsync(stageId);
            if (stage?.Status == StageStatus.Running)
            {
                return stageId;
            }
        }

        return null;
    }

    private void WriteBlock(string header, IReadOnlyDictionary<string, object> rows)
    {
        output.WriteLine(string.Empty);
        output.WriteLine(header);
        output.WriteLine(new string('-', 72));
        foreach (var (label, value) in rows)
        {
            output.WriteLine($"{label,-52} {value}");
        }
    }
}
