// -----------------------------------------------------------------------
// <copyright file="DematerializeChampionshipPostgresPersistenceTests.cs" company="Stéphane ANDRE">
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
public sealed class DematerializeChampionshipPostgresPersistenceTests(PostgresFixture fixture)
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 14, 0, 0, TimeSpan.Zero));

    [IntegrationFact]
    public async Task Dematerialize_after_championship_materialize_persists_without_fixture_xor_violationAsync()
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

            var competition = Competition.Create(new CompetitionName("League PG"), SampleRegulations.Standard(), _clock);
            AddEntry.Execute(competition, "A", _clock);
            AddEntry.Execute(competition, "B", _clock);
            AddEntry.Execute(competition, "C", _clock);
            competitionId = competition.Id;
            competitions.Add(competition);

            var configured = ConfigureStructure.Execute(
                competition,
                null,
                StructureIntent.Championship(),
                _clock);
            stageId = configured.Stage.Id;
            stages.Add(configured.Stage);
            await unitOfWork.SaveChangesAsync();

            var materialized = MaterializeMatches.Execute(competition, configured.Stage, [], _clock);
            materialized.CreatedMatches.Should().HaveCount(3);
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
            var result = DematerializeChampionshipOrGroups.Execute(
                competition,
                stage,
                stageMatches,
                matches,
                _clock);

            result.RemovedMatchCount.Should().Be(3);
            await unitOfWork.SaveChangesAsync();
        }

        using (var scope = fixture.CreateScope())
        {
            var stages = scope.ServiceProvider.GetRequiredService<IStageRepository>();
            var matches = scope.ServiceProvider.GetRequiredService<IMatchRepository>();

            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            stage.Matchdays.Should().ContainSingle().Which.Number.Should().Be(1);
            stage.Matchdays[0].Fixtures.Should().BeEmpty();

            foreach (var matchId in materializedMatchIds)
            {
                (await matches.GetByIdForUpdateAsync(matchId)).Should().BeNull();
            }
        }
    }
}
