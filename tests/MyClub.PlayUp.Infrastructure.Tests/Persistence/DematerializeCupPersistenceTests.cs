// -----------------------------------------------------------------------
// <copyright file="DematerializeCupPersistenceTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class DematerializeCupPersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 14, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Dematerialize_after_cup_materialize_removes_matches_and_leaves_empty_roundsAsync()
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

            var competition = Competition.Create(new CompetitionName("Cup"), SampleRegulations.Standard(), _clock);
            AddEntry.Execute(competition, "Alpha", _clock);
            AddEntry.Execute(competition, "Bravo", _clock);
            competitionId = competition.Id;
            competitions.Add(competition);

            var configured = ConfigureStructure.Execute(
                competition,
                null,
                StructureIntent.Cup(2),
                _clock);
            stageId = configured.Stage.Id;
            var stage = configured.Stage;
            stages.Add(stage);
            var entries = competition.Entries.Select(entry => entry.Id).ToArray();
            stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
            stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
            await unitOfWork.SaveChangesAsync();

            var materialized = MaterializeCupFromOccupiedSlots.Execute(competition, stage, [], [], _clock);
            materialized.CreatedMatches.Should().ContainSingle();
            foreach (var match in materialized.CreatedMatches)
            {
                matches.Add(match);
            }

            materializedMatchIds = materialized.CreatedMatches.Select(match => match.Id).ToArray();
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
            var result = DematerializeCup.Execute(
                competition!,
                stage!,
                stageMatches,
                matches,
                _clock);

            result.RemovedMatchCount.Should().Be(1);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stages = new StageRepository(context);
            var matches = new MatchRepository(context);

            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage.Should().NotBeNull();
            stage!.Rounds.Should().NotBeEmpty();
            stage.Rounds.SelectMany(round => round.Fixtures).Should().BeEmpty();

            foreach (var matchId in materializedMatchIds)
            {
                (await matches.GetByIdForUpdateAsync(matchId)).Should().BeNull();
            }
        }
    }
}
