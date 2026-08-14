// -----------------------------------------------------------------------
// <copyright file="MatchLifecyclePersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class MatchLifecyclePersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 13, 30, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Create_reload_preserves_scheduled_ids_and_null_resultAsync()
    {
        CompetitionId competitionId;
        var stageId = StageId.New();
        var home = EntryId.New();
        var away = EntryId.New();
        MatchId matchId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitionId = competition.Id;
            competitions.Add(competition);

            var match = Match.Create(competitionId, stageId, home, away, _clock);
            matchId = match.Id;
            matches.Add(match);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdAsync(matchId);
            loaded.Should().NotBeNull();
            loaded.CompetitionId.Should().Be(competitionId);
            loaded.StageId.Should().Be(stageId);
            loaded.HomeEntryId.Should().Be(home);
            loaded.AwayEntryId.Should().Be(away);
            loaded.Status.Should().Be(MatchStatus.Scheduled);
            loaded.Result.Should().BeNull();
        }
    }

    [IntegrationFact]
    public async Task Start_finish_preserves_status_and_rich_result_after_reloadAsync()
    {
        MatchId matchId;
        var result = new MatchResult(
            ResultType.Played,
            new Score(2, 2),
            extraTimePlayed: true,
            penaltyShootoutScore: new PenaltyShootoutScore(5, 4));

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var match = Match.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
            match.Start(_clock);
            match.Finish(result, _clock);
            matchId = match.Id;
            matches.Add(match);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IMatchRepository>().GetByIdAsync(matchId);
            loaded.Should().NotBeNull();
            loaded.Status.Should().Be(MatchStatus.Finished);
            loaded.Result.Should().Be(result);
        }
    }

    [IntegrationFact]
    public async Task Postpone_and_cancel_preserve_status_after_reloadAsync()
    {
        MatchId postponedId;
        MatchId cancelledId;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competitions.Add(competition);

            var postponed = Match.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
            postponed.Postpone(_clock);
            postponedId = postponed.Id;
            matches.Add(postponed);

            var cancelled = Match.Create(competition.Id, StageId.New(), EntryId.New(), EntryId.New(), _clock);
            cancelled.Cancel(_clock);
            cancelledId = cancelled.Id;
            matches.Add(cancelled);

            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            (await repository.GetByIdAsync(postponedId))!.Status.Should().Be(MatchStatus.Postponed);
            (await repository.GetByIdAsync(cancelledId))!.Status.Should().Be(MatchStatus.Cancelled);
        }
    }
}
