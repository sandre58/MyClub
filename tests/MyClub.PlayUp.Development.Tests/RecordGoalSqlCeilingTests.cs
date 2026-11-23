// -----------------------------------------------------------------------
// <copyright file="RecordGoalSqlCeilingTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Pipeline;
using MyClub.PlayUp.Development.Runtime;
using MyClub.PlayUp.Development.Scenarios;
using MyClub.PlayUp.Development.Templates;
using MyClub.PlayUp.Development.Tests.Diagnostics;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using Xunit;
using Xunit.Abstractions;

namespace MyClub.PlayUp.Development.Tests;

/// <summary>
/// A10-R7 lot 3 — RecordGoal SQL ceiling with competition lock (lot 1 baseline 80 + lock overhead).
/// </summary>
[Collection("LockMeasurePostgres")]
[Trait("Category", "Integration")]
[Trait("Category", "PerformanceBaseline")]
public sealed class RecordGoalSqlCeilingTests(LockMeasurePostgresFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task Record_goal_sql_stays_within_lot1_baseline_plus_lock_overheadAsync()
    {
        var runner = fixture.Services.GetRequiredService<TemplateRunner>();
        await runner.ResetAndRunAsync([SeedSpec.Parse("champions-league:running")]);

        var live = await PrepareLiveMatchForRecordGoalAsync();

        using var scope = fixture.Services.CreateScope();
        var counter = scope.ServiceProvider.GetRequiredService<SqlCommandCounterInterceptor>();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        counter.Reset();
        await executor.RecordGoalAsync(
            live.MatchId,
            live.ScorerMemberId,
            Side.Home,
            cancellationToken: default);

        output.WriteLine(
            $"RecordGoal SQL={counter.CommandCount}, ceiling={CompetitionLockRetentionMeasureSupport.RecordGoalSqlCeiling} " +
            $"(baseline {CompetitionLockRetentionMeasureSupport.RecordGoalSqlBaselineWithoutLock} + overhead " +
            $"{CompetitionLockRetentionMeasureSupport.RecordGoalSqlLockOverhead})");

        counter.CommandCount.Should().BeLessThanOrEqualTo(
            CompetitionLockRetentionMeasureSupport.RecordGoalSqlCeiling);
    }

    private async Task<LiveMatchTarget> PrepareLiveMatchForRecordGoalAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var executor = scope.ServiceProvider.GetRequiredService<UseCaseExecutor>();
        var competitionId = (await competitions.ListAsync().ConfigureAwait(false)).Single().Id;
        var competition = await competitions.GetByIdReadOnlyAsync(competitionId).ConfigureAwait(false);
        competition.Should().NotBeNull();
        var allMatches = new List<Match>();
        foreach (var stageId in competition!.StageIds)
        {
            allMatches.AddRange(await matches.ListByStageForUpdateAsync(stageId).ConfigureAwait(false));
        }

        var target = allMatches.FirstOrDefault(match => match.Status == MatchStatus.Live)
                     ?? allMatches.First(match => match.Status == MatchStatus.Scheduled);
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        if (target.Status == MatchStatus.Scheduled)
        {
            var scheduledId = target.Id;
            unitOfWork.ClearTrackedChanges();
            await executor.StartMatchAsync(scheduledId).ConfigureAwait(false);
        }

        unitOfWork.ClearTrackedChanges();
        target = (await matches.GetByIdReadOnlyAsync(target.Id).ConfigureAwait(false))!;
        target.DeclaredParticipations.Should().NotBeEmpty();
        return new LiveMatchTarget(target.Id, target.DeclaredParticipations[0].Id);
    }

    private readonly record struct LiveMatchTarget(MatchId MatchId, MemberId ScorerMemberId);
}
