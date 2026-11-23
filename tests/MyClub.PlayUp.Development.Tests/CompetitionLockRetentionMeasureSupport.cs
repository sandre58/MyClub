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

namespace MyClub.PlayUp.Development.Tests;

internal static class CompetitionLockRetentionMeasureSupport
{
    public const int DefaultCiSampleCount = 5;

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

    public static Task AppendConfirmOperationsNotMeasuredAsync(List<SkippedMeasure> skipped)
    {
        foreach (var operation in new[]
                 {
                     "ConfirmCascadeReplaceAsync",
                     "ConfirmResolutionReleaseAsync",
                     "ConfirmDefinitiveResolutionReleaseAsync",
                     "ConfirmDeletedSourceDefinitiveReleaseAsync"
                 })
        {
            skipped.Add(new SkippedMeasure(
                operation,
                "—",
                "nécessite un état de cascade / résolution amont rejouable idempotemment ; absent des scénarios Development.Tests"));
        }

        return Task.CompletedTask;
    }

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

    public static MeasureRow BuildRow(
        string operation,
        string jeu,
        CompetitionShape shape,
        List<TimeSpan> retentions) =>
        new(
            operation,
            jeu,
            shape.TeamCount,
            shape.StageCount,
            shape.SeedMatchCount,
            retentions.Count,
            NearestRankPercentiles.Percentile(retentions, 50),
            NearestRankPercentiles.Percentile(retentions, 95),
            NearestRankPercentiles.Max(retentions),
            NearestRankPercentiles.FractionOverThreshold(retentions, NearestRankPercentiles.ThreeSecondThreshold));
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
    TimeSpan P50,
    TimeSpan P95,
    TimeSpan Max,
    double FractionOverThreeSeconds);

internal readonly record struct ConcurrentRecordGoalReport(
    int Attempts,
    int LockTimeoutCount,
    int InvalidOperationExceptions,
    IReadOnlyList<TimeSpan> LockWaits,
    IReadOnlyList<TimeSpan> EndToEndLatencies,
    IReadOnlyList<string> UnexpectedFailures);

internal readonly record struct SkippedMeasure(string Operation, string Jeu, string Reason);
