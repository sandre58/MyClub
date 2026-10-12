// -----------------------------------------------------------------------
// <copyright file="DematerializeCupPostgresPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

[Collection("postgres")]
[Trait("Category", "Integration")]
public sealed class DematerializeCupPostgresPersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Dematerialize_after_cup_materialize_persists_without_fixture_xor_violationAsync()
    {
        CompetitionId competitionId;
        StageId stageId;
        IReadOnlyList<MatchId> materializedMatchIds;

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = Competition.Create(new CompetitionName("Cup PG"), SampleRegulations.Standard(), _clock);
            AddEntry.Execute(competition, "Alpha", _clock);
            AddEntry.Execute(competition, "Bravo", _clock);
            competitionId = competition.Id;
            competitions.Add(competition);

            var (stage, _, _) = ConfigureStructure.Execute(
                competition,
                null,
                StructureIntent.Cup(2),
                _clock);
            stageId = stage.Id;
            stages.Add(stage);
            var entries = competition.Entries.Select(entry => entry.Id).ToArray();
            stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
            stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
            await unitOfWork.SaveChangesAsync();

            stage.Prepare(_clock);
            var materialized = MaterializeCupFromOccupiedSlots.Execute(competition, stage, [], [], _clock);
            materialized.CreatedMatches.Should().ContainSingle();
            foreach (var match in materialized.CreatedMatches)
            {
                matches.Add(match);
            }

            materializedMatchIds = [.. materialized.CreatedMatches.Select(match => match.Id)];
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var competitions = scope.ServiceProvider.GetRequiredService<ICompetitionRepository>();
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var competition = await competitions.GetByIdForUpdateAsync(competitionId);
            var stage = await stages.GetByIdForUpdateAsync(stageId);
            competition.Should().NotBeNull();
            stage.Should().NotBeNull();

            var stageMatches = await matches.ListByStageForUpdateAsync(stageId);
            var result = DematerializeCup.Execute(
                competition,
                stage,
                stageMatches,
                matches,
                _clock);

            result.RemovedMatchCount.Should().Be(1);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            stage.Rounds.Should().NotBeEmpty();
            stage.Rounds.SelectMany(round => round.Fixtures).Should().BeEmpty();

            foreach (var matchId in materializedMatchIds)
            {
                (await matches.GetByIdForUpdateAsync(matchId)).Should().BeNull();
            }
        }
    }
}
