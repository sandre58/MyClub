// -----------------------------------------------------------------------
// <copyright file="WritePerformanceBaselineTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using System.Runtime.InteropServices;
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
/// Hot-path latency: two warmups, then median of nine measured calls.
/// Product SLO is <see cref="ProductLatencyBudgetMilliseconds"/> ms; the assertion uses
/// <see cref="MedianLatencyCeilingMilliseconds"/> to absorb shared CI runner variance without
/// masking a large regression (local median typically tens of ms; CI median was ~165 ms).
/// </summary>
[Collection("DevelopmentPostgres")]
[Trait("Category", "Integration")]
[Trait("Category", "PerformanceBaseline")]
public sealed class WritePerformanceBaselineTests(
    DevelopmentPostgresFixture fixture,
    ITestOutputHelper output)
{
    private const int WarmupCallCount = 2;
    private const int MeasuredCallCount = 9;
    private const int MaxSqlCommandsPerCall = 20;

    /// <summary>Documented product hot-path latency budget (informational in test output).</summary>
    private const int ProductLatencyBudgetMilliseconds = 150;

    /// <summary>
    /// Strict median ceiling for shared CI runners (regression guard). Product SLO stays
    /// <see cref="ProductLatencyBudgetMilliseconds"/> ms; this ceiling is higher to absorb runner variance.
    /// </summary>
    private const int MedianLatencyCeilingMilliseconds = 200;

    private static readonly TimeSpan MedianLatencyCeiling =
        TimeSpan.FromMilliseconds(MedianLatencyCeilingMilliseconds);

    [Fact]
    public async Task Baseline_replace_qualification_rules_refused_on_running_qf_seedAsync()
    {
        var stageId = await SeedRunningGroupsStageAsync();

        var measuredDurations = new List<TimeSpan>(MeasuredCallCount);
        var measuredSqlCounts = new List<int>(MeasuredCallCount);
        var allSqlCounts = new List<int>(WarmupCallCount + MeasuredCallCount);

        for (var callIndex = 0; callIndex < WarmupCallCount + MeasuredCallCount; callIndex++)
        {
            var sample = await MeasureRefusalCallAsync(stageId);
            allSqlCounts.Add(sample.SqlCommandCount);

            sample.SqlCommandCount.Should().BeLessThanOrEqualTo(
                MaxSqlCommandsPerCall,
                "S2A early status guard must not load QF match graphs before refusal (every call, warmups included)");

            if (callIndex >= WarmupCallCount)
            {
                measuredDurations.Add(sample.Elapsed);
                measuredSqlCounts.Add(sample.SqlCommandCount);
            }
        }

        await AssertStageStillRunningAsync(stageId);

        var median = PerformanceBaselineStatistics.Median(measuredDurations);
        WriteMeasurementReport(measuredDurations, measuredSqlCounts, allSqlCounts, median);

        median.Should().BeLessThan(
            MedianLatencyCeiling,
            $"refusal on Running upstream median must stay under CI ceiling ({MedianLatencyCeilingMilliseconds} ms; product SLO {ProductLatencyBudgetMilliseconds} ms)");
    }

    private async Task<StageId> SeedRunningGroupsStageAsync()
    {
        var runner = fixture.Services.GetRequiredService<ScenarioRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("groups:running")]);

        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var competitionId = (await competitions.ListAsync()).Should().ContainSingle().Subject.Id;
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId);
        competition.Should().NotBeNull();
        var stageId = competition!.StageIds.Should().ContainSingle().Subject;
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var stage = await stages.GetByIdForUpdateAsync(stageId);
        stage.Should().NotBeNull();
        if (stage!.Status is StageStatus.Ready)
        {
            await executor.StartStageAsync(stageId);
            stage = await stages.GetByIdForUpdateAsync(stageId);
        }

        stage!.Status.Should().Be(StageStatus.Running);
        return stageId;
    }

    private async Task AssertStageStillRunningAsync(StageId stageId)
    {
        using var scope = fixture.Services.CreateScope();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var stage = await stages.GetByIdReadOnlyAsync(stageId, StageLoadProfile.Summary);
        stage.Should().NotBeNull();
        stage!.Status.Should().Be(StageStatus.Running, "refused replace must not mutate stage status");
    }

    private async Task<RefusalCallSample> MeasureRefusalCallAsync(StageId stageId)
    {
        using var scope = fixture.Services.CreateScope();
        var counter = scope.ServiceProvider.GetRequiredService<SqlCommandCounterInterceptor>();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        counter.Reset();

        var stopwatch = Stopwatch.StartNew();
        ApplicationFailureException? failure = null;
        try
        {
            await executor.ReplaceStageQualificationRulesAsync(stageId, paths: null);
        }
        catch (ApplicationFailureException ex)
        {
            failure = ex;
        }

        stopwatch.Stop();

        failure.Should().NotBeNull("ReplaceStageQualificationRulesAsync must fail on Running stage");
        failure!.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);

        return new RefusalCallSample(stopwatch.Elapsed, counter.CommandCount);
    }

    private void WriteMeasurementReport(
        List<TimeSpan> measuredDurations,
        List<int> measuredSqlCounts,
        List<int> allSqlCounts,
        TimeSpan median)
    {
        var min = measuredDurations.Min();
        var max = measuredDurations.Max();

        output.WriteLine(string.Empty);
        output.WriteLine("groups:running — ReplaceStageQualificationRulesAsync refused (Running stage)");
        output.WriteLine(new string('-', 72));
        output.WriteLine($"Warmup calls: {WarmupCallCount} (excluded from median)");
        output.WriteLine($"Measured calls: {MeasuredCallCount}");
        output.WriteLine($"Product latency SLO (informational): {ProductLatencyBudgetMilliseconds} ms");
        output.WriteLine($"CI median ceiling (strict): {MedianLatencyCeilingMilliseconds} ms");
        output.WriteLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
        output.WriteLine($"OS: {RuntimeInformation.OSDescription}");
#if DEBUG
        output.WriteLine("Configuration: Debug");
#else
        output.WriteLine("Configuration: Release");
#endif

        output.WriteLine(string.Empty);
        output.WriteLine("Measured durations (ms, high resolution):");
        for (var i = 0; i < measuredDurations.Count; i++)
        {
            output.WriteLine($"  #{i + 1,2}: {measuredDurations[i].TotalMilliseconds,10:F4} ms  SQL={measuredSqlCounts[i]}");
        }

        output.WriteLine(string.Empty);
        output.WriteLine($"Median: {median.TotalMilliseconds:F4} ms");
        output.WriteLine($"Min:    {min.TotalMilliseconds:F4} ms");
        output.WriteLine($"Max:    {max.TotalMilliseconds:F4} ms");

        output.WriteLine(string.Empty);
        output.WriteLine("SQL commands per call (warmups + measured):");
        for (var i = 0; i < allSqlCounts.Count; i++)
        {
            var label = i < WarmupCallCount ? "warmup" : "measured";
            output.WriteLine($"  call {i + 1,2} ({label,-8}): {allSqlCounts[i]}");
        }
    }

    private readonly record struct RefusalCallSample(TimeSpan Elapsed, int SqlCommandCount);
}
