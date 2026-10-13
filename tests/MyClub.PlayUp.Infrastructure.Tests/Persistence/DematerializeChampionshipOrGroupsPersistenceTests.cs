// -----------------------------------------------------------------------
// <copyright file="DematerializeChampionshipOrGroupsPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

/// <summary>
/// Proves dematerialization removes fixture attachments (Restrict on Match) then Match rows in one commit.
/// </summary>
public sealed class DematerializeChampionshipOrGroupsPersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Dematerialize_after_materialize_removes_matches_and_leaves_one_empty_matchdayAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        CompetitionId competitionId;
        StageId stageId;
        IReadOnlyList<MatchId> materializedMatchIds;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var competitions = new CompetitionRepository(context);
            var stages = new StageRepository(context);
            var matches = new MatchRepository(context);
            IUnitOfWork unitOfWork = context;

            var competition = Competition.Create(new CompetitionName("League"), SampleRegulations.Standard(), _clock);
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

            StageLifecycleTestSupport.EnsureReadyForMaterialize(
                competition,
                configured.Stage,
                [configured.Stage],
                _clock);
            var materialized = MaterializeMatches.Execute(
                competition,
                configured.Stage,
                [],
                _clock,
                [configured.Stage]);
            materialized.CreatedMatches.Should().HaveCount(3);
            foreach (var match in materialized.CreatedMatches)
            {
                matches.Add(match);
            }

            materializedMatchIds = [.. materialized.CreatedMatches.Select(match => match.Id)];
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var competitions = new CompetitionRepository(context);
            var stages = new StageRepository(context);
            var matches = new MatchRepository(context);
            IUnitOfWork unitOfWork = context;

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

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stages = new StageRepository(context);
            var matches = new MatchRepository(context);

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
