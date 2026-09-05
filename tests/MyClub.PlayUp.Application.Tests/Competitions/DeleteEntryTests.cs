// -----------------------------------------------------------------------
// <copyright file="DeleteEntryTests.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Competitions;
using MyClub.PlayUp.Application.Tests.Common;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using Xunit;

namespace MyClub.PlayUp.Application.Tests.Competitions;

public sealed class DeleteEntryTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero));
    private readonly RecordingMatchRepository _matches = new();

    [Fact]
    public void DeleteEntry_without_matches_removes_the_row()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var entry = AddEntry.Execute(competition, "Alpha", _clock);

        DeleteEntry.Execute(competition, entry.Id, [], [], _matches, _clock);

        competition.Entries.Should().BeEmpty();
        _matches.Removed.Should().BeEmpty();
    }

    [Fact]
    public void DeleteEntry_with_match_removes_match_then_entry()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var home = AddEntry.Execute(competition, "Alpha", _clock);
        var away = AddEntry.Execute(competition, "Beta", _clock);
        var match = Match.Create(competition.Id, StageId.New(), home.Id, away.Id, _clock);

        DeleteEntry.Execute(competition, home.Id, [], [match], _matches, _clock);

        competition.Entries.Should().ContainSingle(e => e.Id == away.Id);
        _matches.Removed.Should().ContainSingle().Which.Id.Should().Be(match.Id);
    }

    [Fact]
    public void DeleteEntries_deletes_all_and_cascades_matches()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);
        var home = AddEntry.Execute(competition, "Alpha", _clock);
        var away = AddEntry.Execute(competition, "Beta", _clock);
        var match = Match.Create(competition.Id, StageId.New(), home.Id, away.Id, _clock);

        DeleteEntries.Execute(competition, [home.Id, away.Id], [], [match], _matches, _clock);

        competition.Entries.Should().BeEmpty();
        _matches.Removed.Should().ContainSingle().Which.Id.Should().Be(match.Id);
    }

    [Fact]
    public void Empty_lot_is_refused()
    {
        var competition = CreateCompetition.Execute("Cup", _clock);

        var act = () => DeleteEntries.Execute(competition, [], [], [], _matches, _clock);

        act.Should().Throw<ApplicationFailureException>()
            .Which.Code.Should().Be(ApplicationErrorCodes.EmptyEntryLot);
    }

    private sealed class RecordingMatchRepository : IMatchRepository
    {
        public List<Match> Removed { get; } = [];

        public Task<Match?> GetByIdForUpdateAsync(MatchId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Match?> GetByIdReadOnlyAsync(MatchId id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Match>> ListByStageForUpdateAsync(
            StageId stageId,
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

        public void Add(Match match) => throw new NotSupportedException();

        public void Remove(Match match) => Removed.Add(match);
    }
}
