// -----------------------------------------------------------------------
// <copyright file="CompetitionLockRetentionMeasureSupport.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
namespace MyClub.PlayUp.Development.Tests;

internal static class CompetitionLockRetentionMeasureSupport
{
    public const int DefaultCiSampleCount = 5;
    public const int FullCampaignSampleThreshold = 40;

    public static int ResolveSampleCount()
    {
        var raw = Environment.GetEnvironmentVariable("PLAYUP_LOCK_MEASURE_SAMPLES");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultCiSampleCount;
        }

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : DefaultCiSampleCount;
    }

    public static bool IsCiReducedCampaign(int sampleCount) => sampleCount < FullCampaignSampleThreshold;

    public static async Task EmulateExclusiveRecordGoalAsync(
        IServiceScope scope,
        CompetitionId competitionId,
        MatchId matchId,
        MemberId scorerMemberId)
    {
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        await unitOfWork.ExecuteExclusiveAsync(
            competitionId,
            async cancellationToken =>
            {
                await executor.RecordGoalAsync(
                    matchId,
                    scorerMemberId,
                    Side.Home,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }

    public static CompetitionAdvisoryLockTimingSnapshot MeasureExclusiveOperation(
        IServiceScope scope,
        Func<IServiceScope, Task> operation)
    {
        var timing = scope.ServiceProvider.GetRequiredService<CompetitionAdvisoryLockTimingInterceptor>();
        timing.Reset();
        operation(scope).GetAwaiter().GetResult();
        return timing.RequireLastSnapshot();
    }

    public static async Task<CompetitionAdvisoryLockTimingSnapshot> MeasureExclusiveOperationAsync(
        IServiceScope scope,
        Func<IServiceScope, Task> operation)
    {
        var timing = scope.ServiceProvider.GetRequiredService<CompetitionAdvisoryLockTimingInterceptor>();
        timing.Reset();
        await operation(scope).ConfigureAwait(false);
        return timing.RequireLastSnapshot();
    }

    public static async Task<CompetitionShape> LoadShapeFromScopeAsync(IServiceScope scope)
    {
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var listed = await competitions.ListAsync().ConfigureAwait(false);
        var summary = listed.Count switch
        {
            1 => listed[0],
            _ => listed.First(competition => competition.Name.Value.StartsWith("RECETTE G1", StringComparison.Ordinal))
        };
        var competition = await competitions.GetByIdReadOnlyAsync(summary.Id).ConfigureAwait(false);
        competition.Should().NotBeNull();
        var stageId = competition!.StageIds[0];
        var matchCount = 0;
        foreach (var id in competition.StageIds)
        {
            matchCount += (await matches.ListByStageForUpdateAsync(id).ConfigureAwait(false)).Count;
        }

        return new CompetitionShape(
            competition.Id,
            stageId,
            competition.Entries.Count,
            competition.StageIds.Count,
            matchCount);
    }

    public static async Task<CompetitionShape> LoadPrimaryTemplateShapeAsync(IServiceScope scope)
    {
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var listed = await competitions.ListAsync().ConfigureAwait(false);
        var summary = listed.Should().ContainSingle().Subject;
        var competition = await competitions.GetByIdReadOnlyAsync(summary.Id).ConfigureAwait(false);
        competition.Should().NotBeNull();
        var stageId = competition!.StageIds[0];
        var matchCount = 0;
        foreach (var id in competition.StageIds)
        {
            matchCount += (await matches.ListByStageForUpdateAsync(id).ConfigureAwait(false)).Count;
        }

        return new CompetitionShape(
            competition.Id,
            stageId,
            competition.Entries.Count,
            competition.StageIds.Count,
            matchCount);
    }

    public static MeasureRow BuildRow(
        string operation,
        string jeu,
        CompetitionShape shape,
        IReadOnlyList<CompetitionAdvisoryLockTimingSnapshot> snapshots)
    {
        var waits = snapshots.Select(item => item.LockWait).ToList();
        var retentions = snapshots.Select(item => item.LockRetention).ToList();
        return new MeasureRow(
            operation,
            jeu,
            shape.TeamCount,
            shape.StageCount,
            shape.SeedMatchCount,
            snapshots.Count,
            NearestRankPercentiles.Percentile(waits, 50),
            NearestRankPercentiles.Percentile(waits, 95),
            NearestRankPercentiles.Max(waits),
            NearestRankPercentiles.FractionOverThreshold(waits, NearestRankPercentiles.ThreeSecondThreshold),
            NearestRankPercentiles.Percentile(retentions, 50),
            NearestRankPercentiles.Percentile(retentions, 95),
            NearestRankPercentiles.Max(retentions),
            NearestRankPercentiles.FractionOverThreshold(
                retentions,
                NearestRankPercentiles.ThreeSecondThreshold));
    }
}

internal readonly record struct CompetitionShape(
    CompetitionId CompetitionId,
    StageId PrimaryStageId,
    int TeamCount,
    int StageCount,
    int SeedMatchCount);

internal readonly record struct ProgressionTarget(StageId SourceStageId, FixtureId FixtureId);

internal readonly record struct LiveMatchForRecordGoal(
    CompetitionId CompetitionId,
    MatchId MatchId,
    MemberId ScorerMemberId);

internal readonly record struct ConcurrentTargets(
    CompetitionId CompetitionId,
    StageId ProgressionSourceStageId,
    FixtureId ProgressionFixtureId,
    MatchId RecordGoalMatchId,
    MemberId ScorerMemberId);

internal readonly record struct MeasureRow(
    string Operation,
    string Jeu,
    int TeamCount,
    int StageCount,
    int SeedMatchCount,
    int SampleCount,
    TimeSpan WaitP50,
    TimeSpan WaitP95,
    TimeSpan WaitMax,
    double WaitFractionOverThreeSeconds,
    TimeSpan RetentionP50,
    TimeSpan RetentionP95,
    TimeSpan RetentionMax,
    double RetentionFractionOverThreeSeconds);

internal readonly record struct ConcurrentRecordGoalReport(
    int Attempts,
    int LockTimeoutCount,
    int InvalidOperationExceptions,
    IReadOnlyList<TimeSpan> LockWaits,
    IReadOnlyList<TimeSpan> LockRetentions,
    IReadOnlyList<TimeSpan> EndToEndLatencies,
    IReadOnlyList<string> UnexpectedFailures);

internal readonly record struct SkippedMeasure(string Operation, string Jeu, string Reason);
