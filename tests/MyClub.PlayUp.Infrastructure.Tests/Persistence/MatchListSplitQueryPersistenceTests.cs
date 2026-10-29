// -----------------------------------------------------------------------
// <copyright file="MatchListSplitQueryPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Infrastructure.Persistence;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

/// <summary>
/// Guards split-query list loads: no cartesian Include warning, owned collection order preserved.
/// </summary>
[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class MatchListSplitQueryPersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(nameof(IMatchRepository.ListByStageForUpdateAsync))]
    [InlineData(nameof(IMatchRepository.ListByStageIdsForUpdateAsync))]
    [InlineData(nameof(IMatchRepository.ListByStageIdsReadOnlyAsync))]
    public async Task List_loads_do_not_emit_MultipleCollectionIncludeWarningAsync(string method)
    {
        var stageId = await SeedMatchWithSheetFactsAsync();

        using var scope = CreateScope(throwOnCartesianInclude: true);
        var repository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

        Func<Task> act = method switch
        {
            nameof(IMatchRepository.ListByStageForUpdateAsync) => async () =>
            {
                _ = await repository.ListByStageForUpdateAsync(stageId);
            },
            nameof(IMatchRepository.ListByStageIdsForUpdateAsync) => async () =>
            {
                _ = await repository.ListByStageIdsForUpdateAsync([stageId]);
            },
            nameof(IMatchRepository.ListByStageIdsReadOnlyAsync) => async () =>
            {
                _ = await repository.ListByStageIdsReadOnlyAsync([stageId]);
            },
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null)
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListByStageIdsReadOnlyAsync_preserves_owned_collection_order_after_splitAsync()
    {
        var (stageId, matchId, firstGoal, secondGoal, firstSub, secondSub) = await SeedOrderedSheetFactsAsync();

        using var scope = CreateScope(throwOnCartesianInclude: true);
        var listed = await scope.ServiceProvider
            .GetRequiredService<IMatchRepository>()
            .ListByStageIdsReadOnlyAsync([stageId]);
        var loaded = listed[stageId].Single(match => match.Id.Equals(matchId));

        loaded.RecordedGoals.Select(goal => goal.Id).Should().Equal(firstGoal, secondGoal);
        loaded.RecordedSubstitutions.Select(sub => sub.Id).Should().Equal(firstSub, secondSub);
    }

    [Fact]
    public async Task GetByIdsForUpdateAsync_preserves_owned_collection_order_after_splitAsync()
    {
        var (_, matchId, firstGoal, secondGoal, _, _) = await SeedOrderedSheetFactsAsync();

        using var scope = CreateScope(throwOnCartesianInclude: true);
        var loaded = await scope.ServiceProvider
            .GetRequiredService<IMatchRepository>()
            .GetByIdsForUpdateAsync([matchId]);

        loaded.Should().ContainSingle();
        loaded[0].RecordedGoals.Select(goal => goal.Id).Should().Equal(firstGoal, secondGoal);
    }

    [Fact]
    public async Task ListByStageForUpdateAsync_issues_split_query_round_tripsAsync()
    {
        var stageId = await SeedMatchWithSheetFactsAsync();
        var counter = new SqlCommandCounterInterceptor();

        using var scope = CreateScope(sqlCounter: counter);
        counter.Reset();
        _ = await scope.ServiceProvider
            .GetRequiredService<IMatchRepository>()
            .ListByStageForUpdateAsync(stageId);

        counter.CommandCount.Should().BeGreaterThanOrEqualTo(
            5,
            "one principal query plus one split query per owned collection");
    }

    private async Task<StageId> SeedMatchWithSheetFactsAsync()
    {
        using var scope = fixture.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Split guard"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);
        var stage = StageSeed.CreateDraft(competition.Id, _clock);
        var stageId = stage.Id;
        stages.Add(stage);
        SeedMatchWithSheetFacts(competition.Id, stageId, matches);
        await unitOfWork.SaveChangesAsync();
        return stageId;
    }

    private async Task<(StageId StageId, MatchId MatchId, GoalId FirstGoal, GoalId SecondGoal, SubstitutionId FirstSub, SubstitutionId SecondSub)>
        SeedOrderedSheetFactsAsync()
    {
        using var scope = fixture.CreateScope();
        var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
        var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var competition = Competition.Create(new CompetitionName("Split order"), SampleRegulations.Standard(), _clock);
        competitions.Add(competition);
        var stage = StageSeed.CreateDraft(competition.Id, _clock);
        var stageId = stage.Id;
        stages.Add(stage);

        var match = Match.Create(competition.Id, stageId, EntryId.New(), EntryId.New(), _clock);
        var homeA = MemberId.New();
        var homeB = MemberId.New();
        var awayA = MemberId.New();
        var awayB = MemberId.New();
        match.AddDeclaredParticipation(homeA, Side.Home, CompositionStatus.Starter, _clock, 9);
        match.AddDeclaredParticipation(homeB, Side.Home, CompositionStatus.Bench, _clock, 10);
        match.AddDeclaredParticipation(awayA, Side.Away, CompositionStatus.Starter, _clock, 11);
        match.AddDeclaredParticipation(awayB, Side.Away, CompositionStatus.Bench, _clock, 12);
        match.Start(_clock);
        var firstGoal = match.RecordGoal(homeA, Side.Home, _clock).Id;
        var secondGoal = match.RecordGoal(awayA, Side.Away, _clock).Id;
        var firstSub = match.RecordSubstitution(homeA, homeB, Side.Home, _clock).Id;
        var secondSub = match.RecordSubstitution(awayA, awayB, Side.Away, _clock).Id;
        var matchId = match.Id;
        matches.Add(match);
        await unitOfWork.SaveChangesAsync();
        return (stageId, matchId, firstGoal, secondGoal, firstSub, secondSub);
    }

    private IServiceScope CreateScope(
        bool throwOnCartesianInclude = false,
        SqlCommandCounterInterceptor? sqlCounter = null)
    {
        var services = new ServiceCollection();
        services.AddPlayUpInfrastructure(
            fixture.ConnectionString,
            options =>
            {
                if (throwOnCartesianInclude)
                {
                    options.ConfigureWarnings(builder =>
                        builder.Throw(RelationalEventId.MultipleCollectionIncludeWarning));
                }

                if (sqlCounter is not null)
                {
                    options.AddInterceptors(sqlCounter);
                }
            });

        return services.BuildServiceProvider(validateScopes: true).CreateScope();
    }

    private static void SeedMatchWithSheetFacts(CompetitionId competitionId, StageId stageId, IMatchRepository repository)
    {
        var match = Match.Create(competitionId, stageId, EntryId.New(), EntryId.New(), _clock);
        var homeStarter = MemberId.New();
        var homeBench = MemberId.New();
        var awayStarter = MemberId.New();
        match.AddDeclaredParticipation(homeStarter, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(homeBench, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(awayStarter, Side.Away, CompositionStatus.Starter, _clock);
        match.Start(_clock);
        match.RecordGoal(homeStarter, Side.Home, _clock);
        match.RecordSubstitution(homeStarter, homeBench, Side.Home, _clock);
        match.RecordDisciplinaryEvent(homeStarter, DisciplinaryType.Yellow, _clock);
        repository.Add(match);
    }

    private sealed class SqlCommandCounterInterceptor : DbCommandInterceptor
    {
        private int _commandCount;

        public int CommandCount => Volatile.Read(ref _commandCount);

        public void Reset() => Interlocked.Exchange(ref _commandCount, 0);

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            Interlocked.Increment(ref _commandCount);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _commandCount);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
