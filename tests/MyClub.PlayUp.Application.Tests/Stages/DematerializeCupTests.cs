// -----------------------------------------------------------------------
// <copyright file="DematerializeCupTests.cs" company="Stéphane ANDRE">
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

public sealed class DematerializeCupTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero));
    private readonly RecordingMatchRepository _matches = new();

    [Fact]
    public void Dematerialize_removes_all_attached_matches_and_leaves_rounds_without_fixtures()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        var materialized = MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock);
        materialized.CreatedMatches.Should().ContainSingle();
        foreach (var match in materialized.CreatedMatches)
        {
            _matches.Track(match);
        }

        stage.Rounds.Should().NotBeEmpty();
        stage.Rounds[0].Fixtures.Should().NotBeEmpty();

        var result = DematerializeCup.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        result.RemovedMatchCount.Should().Be(1);
        _matches.Removed.Should().ContainSingle();
        stage.Rounds.Should().NotBeEmpty();
        stage.Rounds.SelectMany(round => round.Fixtures).Should().BeEmpty();
        stage.BracketPairs.Should().NotBeEmpty();
        materialized.CreatedMatches.Should().OnlyContain(match => !stage.HasMatch(match.Id));
    }

    [Fact]
    public void Dematerialize_is_idempotent_when_nothing_is_attached()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();

        var result = DematerializeCup.Execute(
            situation.Competition,
            stage,
            [],
            _matches,
            _clock);

        result.RemovedMatchCount.Should().Be(0);
        _matches.Removed.Should().BeEmpty();
    }

    [Fact]
    public void Finished_match_in_scope_blocks_dematerialization()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        var materialized = MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            _matches.Track(match);
        }

        materialized.CreatedMatches[0].Finish(
            new MatchResult(ResultType.Played, new Score(1, 0)),
            _clock);

        var act = () => DematerializeCup.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.DematerializationBlocked);
        stage.Rounds.SelectMany(round => round.Fixtures).Should().NotBeEmpty();
    }

    [Fact]
    public void Running_stage_blocks_dematerialization()
    {
        var situation = TestCompetition.Create("Cup", _clock)
            .WithTeams("Alpha", "Bravo")
            .WithStructure(StructureIntent.Cup(2));
        var stage = situation.RequirePrimaryStage();
        var entries = situation.Competition.Entries.Select(entry => entry.Id).ToArray();
        stage.ApplyResolvedEntry(stage.Slots[0].SlotKey, entries[0], _clock);
        stage.ApplyResolvedEntry(stage.Slots[1].SlotKey, entries[1], _clock);
        var materialized = MaterializeCupFromOccupiedSlots.Execute(
            situation.Competition,
            stage,
            [],
            [],
            _clock);
        foreach (var match in materialized.CreatedMatches)
        {
            _matches.Track(match);
        }

        stage.Prepare(_clock);
        stage.Start(_clock);

        var act = () => DematerializeCup.Execute(
            situation.Competition,
            stage,
            _matches.AllTracked,
            _matches,
            _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.StructureNotMutable);
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
