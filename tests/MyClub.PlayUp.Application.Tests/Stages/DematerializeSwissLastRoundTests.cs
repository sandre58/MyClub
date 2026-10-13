// -----------------------------------------------------------------------
// <copyright file="DematerializeSwissLastRoundTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class DematerializeSwissLastRoundTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 16, 30, 0, TimeSpan.Zero));
    private readonly RecordingMatchRepository _matches = new();

    [Fact]
    public void Dematerialize_last_round_keeps_earlier_rounds_intact()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 4, roundCount: 3);
        var round1 = GenerateNextRound.Execute(competition, stage, [], _clock);
        FinishAll(round1.CreatedMatches);
        Track(round1.CreatedMatches);

        var allMatches = round1.CreatedMatches.ToList();
        var round2 = GenerateNextRound.Execute(competition, stage, allMatches, _clock);
        Track(round2.CreatedMatches);
        allMatches.AddRange(round2.CreatedMatches);

        var result = DematerializeSwissLastRound.Execute(
            competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        result.RemovedMatchCount.Should().Be(2);
        result.RemovedRoundNumber.Should().Be(2);
        stage.Matchdays.Should().ContainSingle().Which.Number.Should().Be(1);
        round1.CreatedMatches.Should().OnlyContain(match => stage.HasMatch(match.Id));
        round2.CreatedMatches.Should().OnlyContain(match => !stage.HasMatch(match.Id));
        _matches.Removed.Should().HaveCount(2);
    }

    [Fact]
    public void Dematerialize_removes_bye_on_last_round()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 3, roundCount: 2);
        var round1 = GenerateNextRound.Execute(competition, stage, [], _clock);
        Track(round1.CreatedMatches);

        var result = DematerializeSwissLastRound.Execute(
            competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        result.RemovedRoundNumber.Should().Be(1);
        stage.SwissByeHistory.Should().BeEmpty();
        stage.Matchdays.Should().BeEmpty();
    }

    [Fact]
    public void Dematerialize_is_no_op_when_no_round_exists()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 4, roundCount: 2);

        var result = DematerializeSwissLastRound.Execute(
            competition,
            stage,
            [],
            _matches,
            _clock);

        result.RemovedMatchCount.Should().Be(0);
        result.RemovedRoundNumber.Should().BeNull();
    }

    [Fact]
    public void Finished_match_on_last_round_blocks_dematerialization()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 4, roundCount: 2);
        var round1 = GenerateNextRound.Execute(competition, stage, [], _clock);
        Track(round1.CreatedMatches);
        round1.CreatedMatches[0].Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);

        var act = () => DematerializeSwissLastRound.Execute(
            competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DematerializationBlocked);
    }

    [Fact]
    public void Orphan_match_blocks_swiss_dematerialization()
    {
        var (competition, stage) = CreateRunningSwiss(participantCount: 4, roundCount: 2);
        var round1 = GenerateNextRound.Execute(competition, stage, [], _clock);
        Track(round1.CreatedMatches);

        var orphan = Match.Create(
            competition.Id,
            stage.Id,
            competition.Entries[0].Id,
            competition.Entries[1].Id,
            _clock);
        _matches.Track(orphan);

        var act = () => DematerializeSwissLastRound.Execute(
            competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationStateInconsistent);
    }

    private void Track(IReadOnlyList<Match> created)
    {
        foreach (var match in created)
        {
            _matches.Track(match);
        }
    }

    private (Competition Competition, Stage Stage) CreateRunningSwiss(int participantCount, int roundCount)
    {
        var competition = CreateCompetition.Execute("Swiss", _clock);
        for (var i = 0; i < participantCount; i++)
        {
            AddEntry.Execute(competition, $"T{i}", _clock);
        }

        var stage = Stage.Create(competition.Id, new StageName("Swiss"), SampleRegulations.Standard(), _clock);
        stage.SetSwissSettings(new SwissSettings(roundCount));
        competition.AddStage(stage.Id, _clock);
        ChampionshipSwissPhasePlaces.FillCompositionToPlacesCapacity(competition, stage, [stage], _clock);
        stage.Prepare(_clock);
        stage.Start(_clock);
        competition.Prepare(_clock);
        competition.Start(_clock);
        return (competition, stage);
    }

    private void FinishAll(IReadOnlyList<Match> matches)
    {
        foreach (var match in matches)
        {
            match.Start(_clock);
            match.Finish(new MatchResult(ResultType.Played, new Score(1, 0)), _clock);
        }
    }

    private sealed class RecordingMatchRepository : IMatchRepository
    {
        private readonly List<Match> _tracked = [];

        public IReadOnlyList<Match> AllTracked => _tracked;

        public List<Match> Removed { get; } = [];

        public void Track(Match match)
        {
            ArgumentNullException.ThrowIfNull(match);
            if (_tracked.All(candidate => !candidate.Id.Equals(match.Id)))
            {
                _tracked.Add(match);
            }
        }

        public Task<Match?> GetByIdForUpdateAsync(MatchId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Match>> GetByIdsForUpdateAsync(
            IReadOnlyList<MatchId> ids,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Match?> GetByIdReadOnlyAsync(MatchId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Match>> ListByStageForUpdateAsync(
            StageId stageId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Match>>([.. _tracked.Where(match => match.StageId.Equals(stageId))]);

        public Task<IReadOnlyDictionary<StageId, IReadOnlyList<Match>>> ListByStageIdsForUpdateAsync(
            IReadOnlyList<StageId> stageIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<StageId, IReadOnlyList<Match>>> ListByStageIdsReadOnlyAsync(
            IReadOnlyList<StageId> stageIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MatchSummaryRow>> ListSummaryRowsByStageReadOnlyAsync(
            StageId stageId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>> ListSummaryRowsByStageIdsReadOnlyAsync(
            IReadOnlyList<StageId> stageIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>>> ListAttentionSlicesByStageIdsReadOnlyAsync(
            IReadOnlyList<StageId> stageIds,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MatchSheetMemberRef>> ListSheetMemberRefsByCompetitionReadOnlyAsync(
            CompetitionId competitionId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Add(Match match) => Track(match);

        public void Remove(Match match)
        {
            Removed.Add(match);
            _tracked.RemoveAll(candidate => candidate.Id.Equals(match.Id));
        }
    }
}
