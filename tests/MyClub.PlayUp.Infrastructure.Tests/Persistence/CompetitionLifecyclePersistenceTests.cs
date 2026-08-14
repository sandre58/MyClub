// -----------------------------------------------------------------------
// <copyright file="CompetitionLifecyclePersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class CompetitionLifecyclePersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 8, 14, 11, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Exclude_start_withdraw_complete_preserves_statuses_after_reloadAsync()
    {
        CompetitionId id;

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competition.AddEntry(TeamId.New(), "Team A", _clock);
            competition.AddEntry(TeamId.New(), "Team B", _clock);
            var excluded = competition.AddEntry(TeamId.New(), "Team C", _clock);
            competition.AddStage(StageId.New(), _clock);
            competition.Prepare(_clock);
            competition.ExcludeEntry(excluded.Id, _clock);
            competition.Start(_clock);
            competition.WithdrawEntry(competition.Entries[1].Id, _clock);
            competition.Complete(CompletionMode.Administrative, _clock);
            id = competition.Id;

            repository.Add(competition);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var loaded = await repository.GetByIdAsync(id);

            loaded.Should().NotBeNull();
            loaded.Status.Should().Be(CompetitionStatus.Completed);
            loaded.CompletionMode.Should().Be(CompletionMode.Administrative);
            loaded.Entries.Should().HaveCount(3);
            loaded.Entries[0].Status.Should().Be(EntryStatus.Active);
            loaded.Entries[1].Status.Should().Be(EntryStatus.Withdrawn);
            loaded.Entries[2].Status.Should().Be(EntryStatus.Excluded);
        }
    }
}
