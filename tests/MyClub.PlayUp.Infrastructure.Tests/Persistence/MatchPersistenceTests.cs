// -----------------------------------------------------------------------
// <copyright file="MatchPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class MatchPersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 13, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Create_reload_preserves_scheduled_ids_and_null_resultAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var competitionId = CompetitionId.New();
        var stageId = StageId.New();
        var home = EntryId.New();
        var away = EntryId.New();
        var match = Match.Create(competitionId, stageId, home, away, _clock);
        var id = match.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new MatchRepository(context).Add(match);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new MatchRepository(context).GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.CompetitionId.Should().Be(competitionId);
            loaded.StageId.Should().Be(stageId);
            loaded.HomeEntryId.Should().Be(home);
            loaded.AwayEntryId.Should().Be(away);
            loaded.Status.Should().Be(MatchStatus.Scheduled);
            loaded.Result.Should().BeNull();
        }
    }

    [Fact]
    public async Task Start_finish_preserves_status_and_result_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var result = new MatchResult(
            ResultType.Played,
            new Score(2, 2),
            extraTimePlayed: true,
            penaltyShootoutScore: new PenaltyShootoutScore(5, 4));
        var id = await SeedFinishedAsync(databaseName, result);

        await using var context = PlayUpInMemory.CreateContext(databaseName);
        var loaded = await new MatchRepository(context).GetByIdAsync(id);

        loaded.Should().NotBeNull();
        loaded.Status.Should().Be(MatchStatus.Finished);
        loaded.Result.Should().Be(result);
    }

    [Fact]
    public async Task Postpone_and_cancel_preserve_status_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        MatchId postponedId;
        MatchId cancelledId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            IUnitOfWork unitOfWork = context;

            var postponed = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
            postponed.Postpone(_clock);
            postponedId = postponed.Id;
            repository.Add(postponed);

            var cancelled = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
            cancelled.Cancel(_clock);
            cancelledId = cancelled.Id;
            repository.Add(cancelled);

            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            (await repository.GetByIdAsync(postponedId))!.Status.Should().Be(MatchStatus.Postponed);
            (await repository.GetByIdAsync(cancelledId))!.Status.Should().Be(MatchStatus.Cancelled);
        }
    }

    [Fact]
    public async Task Finish_marks_result_as_modifiedAsync()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var repository = new MatchRepository(context);
        IUnitOfWork unitOfWork = context;

        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        repository.Add(match);
        await unitOfWork.SaveChangesAsync();

        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        context.Entry(match).Property(candidate => candidate.Result).IsModified.Should().BeTrue();
        context.Entry(match).Property(candidate => candidate.Status).IsModified.Should().BeTrue();
    }

    [Fact]
    public async Task SaveChanges_does_not_clear_domain_eventsAsync()
    {
        await using var context = PlayUpInMemory.CreateContext();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        new MatchRepository(context).Add(match);
        await ((IUnitOfWork)context).SaveChangesAsync();

        match.DomainEvents.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Reload_materializes_empty_domain_eventsAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var id = match.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new MatchRepository(context).Add(match);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new MatchRepository(context).GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.DomainEvents.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Declared_participations_round_trip_with_order_status_and_jerseyAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var starter = match.AddDeclaredParticipation(
            MemberId.New(), Side.Home, CompositionStatus.Starter, _clock, jerseyNumber: 10);
        var bench = match.AddDeclaredParticipation(
            MemberId.New(), Side.Away, CompositionStatus.Bench, _clock);
        var removed = match.AddDeclaredParticipation(
            MemberId.New(), Side.Home, CompositionStatus.Bench, _clock, jerseyNumber: 7);
        match.RemoveDeclaredParticipation(removed.Id, _clock);
        var id = match.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new MatchRepository(context).Add(match);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            var loaded = await repository.GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.DeclaredParticipations.Select(p => p.Id).Should().Equal(starter.Id, bench.Id);
            loaded.DeclaredParticipations[0].Side.Should().Be(Side.Home);
            loaded.DeclaredParticipations[0].CompositionStatus.Should().Be(CompositionStatus.Starter);
            loaded.DeclaredParticipations[0].JerseyNumber.Should().Be(10);
            loaded.DeclaredParticipations[1].Side.Should().Be(Side.Away);
            loaded.DeclaredParticipations[1].CompositionStatus.Should().Be(CompositionStatus.Bench);
            loaded.DeclaredParticipations[1].JerseyNumber.Should().BeNull();

            loaded.ChangeDeclaredParticipationCompositionStatus(starter.Id, CompositionStatus.Bench, _clock);
            loaded.SetDeclaredParticipationJerseyNumber(bench.Id, 99, _clock);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new MatchRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.DeclaredParticipations.Should().HaveCount(2);
            reloaded.DeclaredParticipations.Single(p => p.Id == starter.Id).CompositionStatus
                .Should().Be(CompositionStatus.Bench);
            reloaded.DeclaredParticipations.Single(p => p.Id == bench.Id).JerseyNumber.Should().Be(99);
        }
    }

    [Fact]
    public async Task Finish_keeps_declared_participations_after_reloadAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var memberId = MemberId.New();
        match.AddDeclaredParticipation(memberId, Side.Home, CompositionStatus.Starter, _clock, 9);
        match.Start(_clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var id = match.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new MatchRepository(context).Add(match);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new MatchRepository(context).GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.Status.Should().Be(MatchStatus.Finished);
            loaded.DeclaredParticipations.Should().ContainSingle()
                .Which.Id.Should().Be(memberId);
        }
    }

    [Fact]
    public async Task Running_score_round_trips_and_survives_finishAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        match.Start(_clock);
        match.SetRunningScore(new RunningScore(2, 1), _clock);
        var id = match.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new MatchRepository(context).Add(match);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            var loaded = await repository.GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.Status.Should().Be(MatchStatus.Live);
            loaded.RunningScore.Should().Be(new RunningScore(2, 1));

            loaded.SetRunningScore(new RunningScore(2, 2), _clock);
            loaded.Finish(new MatchResult(ResultType.Played, new Score(3, 2)), _clock);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new MatchRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.Status.Should().Be(MatchStatus.Finished);
            reloaded.RunningScore.Should().Be(new RunningScore(2, 2));
            reloaded.Result!.Score.Should().Be(new Score(3, 2));
        }
    }

    [Fact]
    public async Task Scheduled_match_persists_null_running_scoreAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var id = match.Id;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            new MatchRepository(context).Add(match);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var loaded = await new MatchRepository(context).GetByIdAsync(id);
            loaded.Should().NotBeNull();
            loaded.RunningScore.Should().BeNull();
        }
    }

    private async Task<MatchId> SeedFinishedAsync(string databaseName, MatchResult result)
    {
        await using var context = PlayUpInMemory.CreateContext(databaseName);
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        match.Start(_clock);
        match.Finish(result, _clock);
        new MatchRepository(context).Add(match);
        await ((IUnitOfWork)context).SaveChangesAsync();
        return match.Id;
    }
}
