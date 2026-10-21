// -----------------------------------------------------------------------
// <copyright file="DematerializeSwissLastRoundPersistenceTests.cs" company="Stéphane ANDRE">
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
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Infrastructure.Persistence.Repositories;
using MyClub.PlayUp.Infrastructure.Tests.Common;
using Xunit;

namespace MyClub.PlayUp.Infrastructure.Tests.Persistence;

public sealed class DematerializeSwissLastRoundPersistenceTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 17, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Dematerialize_last_swiss_round_persists_match_removalAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        CompetitionId competitionId;
        StageId stageId;
        MatchId removedMatchId;
        MatchId keptMatchId;

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var competitions = new CompetitionRepository(context);
            var stages = new StageRepository(context);
            var matches = new MatchRepository(context);
            IUnitOfWork unitOfWork = context;

            var competition = Competition.Create(new CompetitionName("Swiss"), SampleRegulations.Standard(), _clock);
            for (var i = 0; i < 4; i++)
            {
                AddEntry.Execute(competition, $"T{i}", _clock);
            }

            competitionId = competition.Id;
            competitions.Add(competition);

            var stage = Stage.Create(competition.Id, new StageName("Swiss"), SampleRegulations.Standard(), _clock);
            stage.SetSwissSettings(new SwissSettings(3));
            competition.AddStage(stage.Id, _clock);
            foreach (var entry in competition.Entries.Where(entry => entry.Status == EntryStatus.Active))
            {
                stage.AddResolvedPopulationEntry(entry.Id, _clock);
            }

            stage.Prepare(_clock);
            stage.Start(_clock);
            competition.Prepare(_clock);
            competition.Start(_clock);
            stageId = stage.Id;
            stages.Add(stage);
            await unitOfWork.SaveChangesAsync();

            var round1 = GenerateNextRound.Execute(competition, stage, [], _clock, [stage]);
            foreach (var match in round1.CreatedMatches)
            {
                match.Start(_clock);
                match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
                matches.Add(match);
            }

            keptMatchId = round1.CreatedMatches[0].Id;
            var round2 = GenerateNextRound.Execute(
                competition,
                stage,
                [.. round1.CreatedMatches],
                _clock,
                [stage]);
            foreach (var match in round2.CreatedMatches)
            {
                matches.Add(match);
            }

            removedMatchId = round2.CreatedMatches[0].Id;
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
            var stageMatches = await matches.ListByStageForUpdateAsync(stageId);

            var result = DematerializeSwissLastRound.Execute(
                competition!,
                stage!,
                stageMatches,
                matches,
                _clock);

            result.RemovedMatchCount.Should().Be(2);
            result.RemovedRoundNumber.Should().Be(2);
            await unitOfWork.SaveChangesAsync();
        }

        await using (var context = PlayUpInMemory.CreateContext(databaseName))
        {
            var stages = new StageRepository(context);
            var matches = new MatchRepository(context);

            var stage = await stages.GetByIdForUpdateAsync(stageId);
            stage!.Matchdays.Should().ContainSingle().Which.Number.Should().Be(1);

            (await matches.GetByIdForUpdateAsync(removedMatchId)).Should().BeNull();
            (await matches.GetByIdForUpdateAsync(keptMatchId)).Should().NotBeNull();
        }
    }
}
