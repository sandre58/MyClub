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
    public async Task Delete_start_withdraw_complete_preserves_statuses_after_reloadAsync()
    {
        CompetitionId id;

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
            competition.AddEntry(TeamId.New(), "Team A", _clock);
            competition.AddEntry(TeamId.New(), "Team B", _clock);
            var removed = competition.AddEntry(TeamId.New(), "Team C", _clock);

            var stage = StageSeed.CreateDraft(competition.Id, _clock, "Main");
            scope.ServiceProvider.GetRequiredService<IStageRepository>().Add(stage);
            competition.AddStage(stage.Id, _clock);
            competition.Prepare(_clock);
            competition.DeleteEntry(removed.Id, _clock);
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
            var loaded = await repository.GetByIdForUpdateAsync(id);

            loaded.Should().NotBeNull();
            loaded.Status.Should().Be(CompetitionStatus.Completed);
            loaded.CompletionMode.Should().Be(CompletionMode.Administrative);
            loaded.Entries.Should().HaveCount(2);
            loaded.Entries[0].Status.Should().Be(EntryStatus.Active);
            loaded.Entries[1].Status.Should().Be(EntryStatus.Withdrawn);
        }
    }

    [IntegrationFact]
    public async Task Regulation_round_trips_and_replace_persists_on_postgresAsync()
    {
        CompetitionId id;
        var initial = SampleRegulations.Standard();
        var replacement = SampleRegulations.WithExtraTimeAndShootout();

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup"), initial, _clock);
            id = competition.Id;
            repository.Add(competition);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var repository = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var loaded = await repository.GetByIdForUpdateAsync(id);
            loaded.Should().NotBeNull();
            loaded.Regulation.Should().Be(initial);
            loaded.ReplaceRegulation(replacement, _clock);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var reloaded = await scope.ServiceProvider.GetRequiredService<ICompetitionRepository>().GetByIdForUpdateAsync(id);
            reloaded.Should().NotBeNull();
            reloaded.Regulation.Should().Be(replacement);
            reloaded.Regulation.MatchRules.ExtraTimePolicy.Should().NotBeNull();
            reloaded.Regulation.MatchRules.PenaltyShootoutPolicy.Should().NotBeNull();
        }
    }
}
