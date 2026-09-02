// -----------------------------------------------------------------------
// <copyright file="MatchPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;
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
    public async Task Same_member_can_be_declared_on_two_matches_in_one_saveAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var member = MemberId.New();
        var first = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var second = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        first.AddDeclaredParticipation(member, Side.Home, CompositionStatus.Starter, _clock, 10);
        second.AddDeclaredParticipation(member, Side.Away, CompositionStatus.Bench, _clock, 10);

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            repository.Add(first);
            repository.Add(second);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var repository = new MatchRepository(context);
            var loadedFirst = await repository.GetByIdAsync(first.Id);
            var loadedSecond = await repository.GetByIdAsync(second.Id);
            loadedFirst.Should().NotBeNull();
            loadedSecond.Should().NotBeNull();
            loadedFirst.DeclaredParticipations.Should().ContainSingle(p => p.Id.Equals(member));
            loadedSecond.DeclaredParticipations.Should().ContainSingle(p => p.Id.Equals(member));
        }
    }

    [Fact]
    public async Task Recorded_goals_round_trip_with_order_assister_own_goal_and_correctionAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var dupont = MemberId.New();
        var martin = MemberId.New();
        var rossi = MemberId.New();
        match.AddDeclaredParticipation(dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(martin, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(rossi, Side.Away, CompositionStatus.Starter, _clock);
        match.Start(_clock);

        var withAssister = match.RecordGoal(dupont, Side.Home, _clock, assisterMemberId: martin);
        var ownGoal = match.RecordGoal(dupont, Side.Away, _clock);
        var removed = match.RecordGoal(rossi, Side.Away, _clock);
        match.RemoveRecordedGoal(removed.Id, _clock);
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
            loaded.RecordedGoals.Select(g => g.Id).Should().Equal(withAssister.Id, ownGoal.Id);
            loaded.RecordedGoals[0].ScorerMemberId.Should().Be(dupont);
            loaded.RecordedGoals[0].CreditedSide.Should().Be(Side.Home);
            loaded.RecordedGoals[0].AssisterMemberId.Should().Be(martin);
            loaded.RecordedGoals[1].CreditedSide.Should().Be(Side.Away);
            loaded.RecordedGoals[1].AssisterMemberId.Should().BeNull();
            loaded.IsOwnGoal(loaded.RecordedGoals[1]).Should().BeTrue();
            loaded.RunningScore.Should().Be(new RunningScore(0, 0));

            loaded.CorrectRecordedGoal(withAssister.Id, martin, Side.Home, _clock);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new MatchRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.RecordedGoals.Should().HaveCount(2);
            var corrected = reloaded.RecordedGoals.Single(g => g.Id == withAssister.Id);
            corrected.ScorerMemberId.Should().Be(martin);
            corrected.AssisterMemberId.Should().BeNull();
        }
    }

    [Fact]
    public async Task Finish_keeps_recorded_goals_and_allows_correction_round_tripAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var dupont = MemberId.New();
        var martin = MemberId.New();
        match.AddDeclaredParticipation(dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(martin, Side.Home, CompositionStatus.Bench, _clock);
        match.Start(_clock);
        var goal = match.RecordGoal(dupont, Side.Home, _clock);
        match.SetRunningScore(new RunningScore(1, 0), _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 0)), _clock);
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
            loaded.Status.Should().Be(MatchStatus.Finished);
            loaded.RecordedGoals.Should().ContainSingle().Which.Id.Should().Be(goal.Id);
            loaded.Result!.Score.Should().Be(new Score(2, 0));
            loaded.RunningScore.Should().Be(new RunningScore(1, 0));

            loaded.CorrectRecordedGoal(goal.Id, martin, Side.Home, _clock);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new MatchRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.RecordedGoals.Single().ScorerMemberId.Should().Be(martin);
            reloaded.Result!.Score.Should().Be(new Score(2, 0));
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

    [Fact]
    public async Task Recorded_substitutions_round_trip_preserves_order_and_correctionAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var dupont = MemberId.New();
        var martin = MemberId.New();
        var bernard = MemberId.New();
        match.AddDeclaredParticipation(dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(martin, Side.Home, CompositionStatus.Bench, _clock);
        match.AddDeclaredParticipation(bernard, Side.Home, CompositionStatus.Bench, _clock);
        match.Start(_clock);

        var first = match.RecordSubstitution(dupont, martin, Side.Home, _clock);
        var second = match.RecordSubstitution(martin, bernard, Side.Home, _clock);
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
            loaded.RecordedSubstitutions.Select(s => s.Id).Should().Equal(first.Id, second.Id);
            loaded.RecordedSubstitutions[0].OutMemberId.Should().Be(dupont);
            loaded.RecordedSubstitutions[0].InMemberId.Should().Be(martin);
            loaded.RecordedSubstitutions[1].OutMemberId.Should().Be(martin);
            loaded.RecordedSubstitutions[1].InMemberId.Should().Be(bernard);
            loaded.DeclaredParticipations.Single(p => p.Id == dupont).CompositionStatus
                .Should().Be(CompositionStatus.Starter);

            loaded.RemoveRecordedSubstitution(second.Id, _clock);
            loaded.CorrectRecordedSubstitution(first.Id, dupont, bernard, Side.Home, _clock);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new MatchRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.RecordedSubstitutions.Should().ContainSingle();
            reloaded.RecordedSubstitutions[0].Id.Should().Be(first.Id);
            reloaded.RecordedSubstitutions[0].InMemberId.Should().Be(bernard);
            reloaded.DeclaredParticipations.Single(p => p.Id == dupont).CompositionStatus
                .Should().Be(CompositionStatus.Starter);
        }
    }

    [Fact]
    public async Task Finished_without_live_substitution_reconstruction_round_tripAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var dupont = MemberId.New();
        var martin = MemberId.New();
        match.AddDeclaredParticipation(dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(martin, Side.Home, CompositionStatus.Bench, _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(2, 1)), _clock);
        var sub = match.RecordSubstitution(dupont, martin, Side.Home, _clock);
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
            loaded.HasObservedLive.Should().BeFalse();
            loaded.RunningScore.Should().BeNull();
            loaded.RecordedSubstitutions.Should().ContainSingle().Which.Id.Should().Be(sub.Id);
            loaded.Status.Should().Be(MatchStatus.Finished);
        }
    }

    [Fact]
    public async Task Recorded_disciplinary_events_round_trip_preserves_order_and_correctionAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var dupont = MemberId.New();
        var martin = MemberId.New();
        match.AddDeclaredParticipation(dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.AddDeclaredParticipation(martin, Side.Home, CompositionStatus.Bench, _clock);
        match.Start(_clock);

        var yellow = match.RecordDisciplinaryEvent(dupont, DisciplinaryType.Yellow, _clock);
        var white = match.RecordDisciplinaryEvent(martin, DisciplinaryType.White, _clock);
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
            loaded.RecordedDisciplinaryEvents.Select(e => e.Id).Should().Equal(yellow.Id, white.Id);
            loaded.RecordedDisciplinaryEvents[0].Type.Should().Be(DisciplinaryType.Yellow);
            loaded.RecordedDisciplinaryEvents[0].MemberId.Should().Be(dupont);
            loaded.RecordedDisciplinaryEvents[1].Type.Should().Be(DisciplinaryType.White);
            loaded.RecordedSubstitutions.Should().BeEmpty();
            loaded.RunningScore.Should().Be(new RunningScore(0, 0));

            loaded.RemoveRecordedDisciplinaryEvent(white.Id, _clock);
            loaded.CorrectRecordedDisciplinaryEvent(yellow.Id, dupont, DisciplinaryType.Red, _clock);
            await ((IUnitOfWork)context).SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var reloaded = await new MatchRepository(context).GetByIdAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.RecordedDisciplinaryEvents.Should().ContainSingle();
            reloaded.RecordedDisciplinaryEvents[0].Id.Should().Be(yellow.Id);
            reloaded.RecordedDisciplinaryEvents[0].Type.Should().Be(DisciplinaryType.Red);
            reloaded.RecordedSubstitutions.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task Finished_without_live_disciplinary_reconstruction_round_tripAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        var match = Match.Create(CompetitionId.New(), StageId.New(), EntryId.New(), EntryId.New(), _clock);
        var dupont = MemberId.New();
        match.AddDeclaredParticipation(dupont, Side.Home, CompositionStatus.Starter, _clock);
        match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        var evt = match.RecordDisciplinaryEvent(dupont, DisciplinaryType.Yellow, _clock);
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
            loaded.HasObservedLive.Should().BeFalse();
            loaded.RunningScore.Should().BeNull();
            loaded.RecordedDisciplinaryEvents.Should().ContainSingle().Which.Id.Should().Be(evt.Id);
            loaded.Status.Should().Be(MatchStatus.Finished);
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
