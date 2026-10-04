// -----------------------------------------------------------------------
// <copyright file="DematerializeChampionshipOrGroupsTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.TestKit;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Stages;

public sealed class DematerializeChampionshipOrGroupsTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingMatchRepository _matches = new();

    [Fact]
    public void Dematerialize_removes_all_attached_matches_and_trims_to_one_empty_matchday()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C", "D")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        materialized.CreatedMatches.Should().HaveCount(6);
        foreach (var match in materialized.CreatedMatches)
        {
            _matches.Track(match);
        }

        stage.Matchdays.Should().HaveCountGreaterThan(1);

        var result = DematerializeChampionshipOrGroups.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        result.RemovedMatchCount.Should().Be(6);
        _matches.Removed.Should().HaveCount(6);
        stage.Matchdays.Should().ContainSingle().Which.Number.Should().Be(1);
        stage.Matchdays[0].Fixtures.Should().BeEmpty();
        materialized.CreatedMatches.Should().OnlyContain(match => !stage.HasMatch(match.Id));
    }

    [Fact]
    public void Dematerialize_is_idempotent_when_nothing_is_attached()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();

        var result = DematerializeChampionshipOrGroups.Execute(
            situation.Competition,
            stage,
            [],
            _matches,
            _clock);

        result.RemovedMatchCount.Should().Be(0);
        _matches.Removed.Should().BeEmpty();
    }

    [Fact]
    public void Second_dematerialize_after_first_returns_zero()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            _matches.Track(match);
        }

        DematerializeChampionshipOrGroups.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        var second = DematerializeChampionshipOrGroups.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        second.RemovedMatchCount.Should().Be(0);
    }

    [Fact]
    public void Orphan_match_refuses_dematerialization()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            _matches.Track(match);
        }

        var orphan = Match.Create(
            situation.Competition.Id,
            stage.Id,
            situation.Competition.Entries[0].Id,
            situation.Competition.Entries[1].Id,
            _clock);
        _matches.Track(orphan);

        var act = () => DematerializeChampionshipOrGroups.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.MaterializationStateInconsistent);
        _matches.Removed.Should().BeEmpty();
    }

    [Fact]
    public void Finished_match_in_scope_blocks_dematerialization()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B", "C")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            _matches.Track(match);
        }

        materialized.CreatedMatches[0].Finish(
            new MatchResult(ResultType.Played, new Score(1, 0)),
            _clock);

        var act = () => DematerializeChampionshipOrGroups.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DematerializationBlocked);
        stage.Matchdays.SelectMany(matchday => matchday.Fixtures).Should().NotBeEmpty();
    }

    [Fact]
    public void Cancelled_without_live_does_not_block_dematerialization()
    {
        var situation = TestCompetition.Create("Champ", _clock)
            .WithTeams("A", "B")
            .WithStructure(StructureIntent.Championship());
        var stage = situation.RequirePrimaryStage();
        var materialized = MaterializeMatches.Execute(situation.Competition, stage, [], _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            _matches.Track(match);
        }

        materialized.CreatedMatches[0].Cancel(_clock);

        var result = DematerializeChampionshipOrGroups.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        result.RemovedMatchCount.Should().Be(materialized.CreatedMatches.Count);
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
