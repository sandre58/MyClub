// -----------------------------------------------------------------------
// <copyright file="MatchReadAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Assembles match list and detail product views (no Winner, no Domain mutation).
/// </summary>
public static class MatchReadAssembler
{
    /// <summary>
    /// Assembles match summaries for a stage, ordered by round/fixture/leg then MatchId.
    /// </summary>
    /// <param name="stage">Stage that owns the fixtures.</param>
    /// <param name="competition">Competition for display names.</param>
    /// <param name="matches">Matches loaded for the stage.</param>
    /// <returns>Ordered match summaries.</returns>
    public static IReadOnlyList<MatchSummaryDto> AssembleSummaries(
        Stage stage,
        Competition competition,
        IReadOnlyList<Match> matches)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(matches);

        var entries = EntryDisplayNames.ToEntries(competition);
        var placement = BuildFixturePlacementIndex(stage);
        var origins = BuildSlotOrigins(stage);

        return
        [
            .. matches
                .OrderBy(match => placement.TryGetValue(match.Id, out var info) ? info.Sequence : int.MaxValue)
                .ThenBy(match => match.Id.Value)
                .Select(match =>
                {
                    ResolveCalendarPlacement(stage, match.Id, out var scheduledAt, out var resourceId);
                    placement.TryGetValue(match.Id, out var info);
                    origins.TryGetValue(match.Id, out var origin);

                    return new MatchSummaryDto(
                        match.Id.Value,
                        match.StageId.Value,
                        match.Status,
                        EntryDisplayNames.ToSide(entries, match.HomeEntryId, origin.HomeSlotKey),
                        EntryDisplayNames.ToSide(entries, match.AwayEntryId, origin.AwaySlotKey),
                        MapScore(match.Result),
                        info.FixtureId?.Value,
                        info.RoundId?.Value,
                        scheduledAt,
                        resourceId,
                        info.MatchdayNumber,
                        info.RoundName,
                        match.Result?.Type);
                })
        ];
    }

    /// <summary>
    /// Assembles match summaries from projected rows (list endpoint).
    /// </summary>
    /// <param name="stage">Stage that owns the fixtures.</param>
    /// <param name="competition">Competition for display names.</param>
    /// <param name="rows">Projected match rows.</param>
    /// <returns>Ordered match summaries.</returns>
    public static IReadOnlyList<MatchSummaryDto> AssembleSummaries(
        Stage stage,
        Competition competition,
        IReadOnlyList<MatchSummaryRow> rows)
    {
        ArgumentNullException.ThrowIfNull(stage);
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(rows);

        var entries = EntryDisplayNames.ToEntries(competition);
        var placement = BuildFixturePlacementIndex(stage);
        var origins = BuildSlotOrigins(stage);

        return
        [
            .. rows
                .OrderBy(row => placement.TryGetValue(row.Id, out var info) ? info.Sequence : int.MaxValue)
                .ThenBy(row => row.Id.Value)
                .Select(row =>
                {
                    ResolveCalendarPlacement(stage, row.Id, out var scheduledAt, out var resourceId);
                    placement.TryGetValue(row.Id, out var info);
                    origins.TryGetValue(row.Id, out var origin);

                    return new MatchSummaryDto(
                        row.Id.Value,
                        row.StageId.Value,
                        row.Status,
                        EntryDisplayNames.ToSide(entries, row.HomeEntryId, origin.HomeSlotKey),
                        EntryDisplayNames.ToSide(entries, row.AwayEntryId, origin.AwaySlotKey),
                        MapScore(row.Result),
                        info.FixtureId?.Value,
                        info.RoundId?.Value,
                        scheduledAt,
                        resourceId,
                        info.MatchdayNumber,
                        info.RoundName,
                        row.Result?.Type);
                })
        ];
    }

    /// <summary>
    /// Assembles match detail (fixture/leg when present on the stage).
    /// </summary>
    /// <param name="match">Loaded match.</param>
    /// <param name="competition">Competition for display names.</param>
    /// <param name="stage">Owning stage when loaded; otherwise <see langword="null"/>.</param>
    /// <returns>The assembled detail.</returns>
    public static MatchDetailDto AssembleDetail(Match match, Competition competition, Stage? stage)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(competition);

        var entries = EntryDisplayNames.ToEntries(competition);
        var origins = stage is null ? [] : BuildSlotOrigins(stage);
        origins.TryGetValue(match.Id, out var origin);
        DateTimeOffset? scheduledAt = null;
        Guid? resourceId = null;
        Guid? fixtureId = null;
        int? legIndex = null;

        if (stage is null)
        {
            return BuildDetail(
                match,
                entries,
                fixtureId,
                legIndex,
                scheduledAt,
                resourceId,
                origin);
        }

        ResolveCalendarPlacement(stage, match.Id, out scheduledAt, out resourceId);
        var attachment = FindAttachment(stage, match.Id);
        if (attachment is not { } found)
        {
            return BuildDetail(
                match,
                entries,
                fixtureId,
                legIndex,
                scheduledAt,
                resourceId,
                origin);
        }

        fixtureId = found.FixtureId.Value;
        legIndex = found.LegIndex;

        return BuildDetail(
            match,
            entries,
            fixtureId,
            legIndex,
            scheduledAt,
            resourceId,
            origin);
    }

    /// <summary>
    /// Builds Consultation-compatible context label from projected summary fields.
    /// Matchday labels stay FR for wire stability of <see cref="ConsultationResultDto"/>.
    /// </summary>
    public static string? ConsultationContextLabel(MatchSummaryDto summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return summary.RoundName is { Length: > 0 } ? summary.RoundName : summary.MatchdayNumber is { } number ? $"Journée {number}" : null;
    }

    private readonly record struct MatchSlotOrigin(string? HomeSlotKey, string? AwaySlotKey);

    private static Dictionary<MatchId, MatchSlotOrigin> BuildSlotOrigins(Stage stage)
    {
        var map = new Dictionary<MatchId, MatchSlotOrigin>();
        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                foreach (var attachment in fixture.Attachments)
                {
                    var slotAIsHome = attachment.LegIndex != 2;
                    map[attachment.MatchId] = new MatchSlotOrigin(
                        slotAIsHome ? fixture.SlotAKey : fixture.SlotBKey,
                        slotAIsHome ? fixture.SlotBKey : fixture.SlotAKey);
                }
            }
        }

        return map;
    }

    private static void ResolveCalendarPlacement(
        Stage stage,
        MatchId matchId,
        out DateTimeOffset? scheduledAt,
        out Guid? resourceId)
    {
        if (stage.TryGetMatchPlacement(matchId, out var placement))
        {
            scheduledAt = placement.Start;
            resourceId = placement.ResourceId.Value;
            return;
        }

        scheduledAt = null;
        resourceId = null;
    }

    private static MatchScoreDto? MapScore(MatchResult? result) =>
        result is null ? null : new MatchScoreDto(result.Score.HomeGoals, result.Score.AwayGoals);

    private static MatchScoreDto? MapRunningScore(RunningScore? runningScore) =>
        runningScore is { } score ? new MatchScoreDto(score.HomeGoals, score.AwayGoals) : null;

    private static MatchDetailDto BuildDetail(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        Guid? fixtureId,
        int? legIndex,
        DateTimeOffset? scheduledAt,
        Guid? resourceId,
        MatchSlotOrigin origin = default) =>
        new(
            match.Id.Value,
            match.CompetitionId.Value,
            match.StageId.Value,
            match.Status,
            EntryDisplayNames.ToSide(entries, match.HomeEntryId, origin.HomeSlotKey),
            EntryDisplayNames.ToSide(entries, match.AwayEntryId, origin.AwaySlotKey),
            MapResult(match.Result),
            fixtureId,
            legIndex,
            scheduledAt,
            resourceId,
            match.HasObservedLive,
            MapRunningScore(match.RunningScore),
            MapDeclaredParticipations(match, entries),
            MapRecordedGoals(match, entries),
            MapRecordedSubstitutions(match, entries),
            MapRecordedDisciplinaryEvents(match, entries));

    private static IReadOnlyList<DeclaredParticipationDto> MapDeclaredParticipations(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries) =>
    [
        .. match.DeclaredParticipations.Select(participation => new DeclaredParticipationDto(
            participation.Id.Value,
            ResolveSheetMemberDisplayName(match, entries, participation.Id),
            participation.Side,
            participation.CompositionStatus,
            participation.JerseyNumber))
    ];

    private static IReadOnlyList<RecordedGoalDto> MapRecordedGoals(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries) =>
    [
        .. match.RecordedGoals.Select(goal => MapRecordedGoal(match, entries, goal))
    ];

    private static RecordedGoalDto MapRecordedGoal(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        RecordedGoal goal)
    {
        var assisterDisplayName = goal.AssisterMemberId is { } assister
            ? ResolveSheetMemberDisplayName(match, entries, assister)
            : null;

        return new RecordedGoalDto(
            goal.Id.Value,
            goal.ScorerMemberId.Value,
            ResolveSheetMemberDisplayName(match, entries, goal.ScorerMemberId),
            goal.CreditedSide,
            goal.AssisterMemberId?.Value,
            assisterDisplayName,
            match.IsOwnGoal(goal));
    }

    private static IReadOnlyList<RecordedSubstitutionDto> MapRecordedSubstitutions(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries) =>
    [
        .. match.RecordedSubstitutions.Select(substitution => new RecordedSubstitutionDto(
            substitution.Id.Value,
            substitution.Side,
            substitution.OutMemberId.Value,
            ResolveSideMemberDisplayName(match, entries, substitution.Side, substitution.OutMemberId),
            substitution.InMemberId.Value,
            ResolveSideMemberDisplayName(match, entries, substitution.Side, substitution.InMemberId)))
    ];

    private static IReadOnlyList<RecordedDisciplinaryEventDto> MapRecordedDisciplinaryEvents(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries) =>
    [
        .. match.RecordedDisciplinaryEvents.Select(evt => new RecordedDisciplinaryEventDto(
            evt.Id.Value,
            evt.MemberId.Value,
            ResolveSheetMemberDisplayName(match, entries, evt.MemberId),
            evt.Type))
    ];

    private static EntryId? EntryIdForSide(Match match, Side side) =>
        side == Side.Home ? match.HomeEntryId : match.AwayEntryId;

    private static string? ResolveSideMemberDisplayName(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        Side side,
        MemberId memberId)
    {
        var entryId = EntryIdForSide(match, side);
        return entryId is { } id
            ? EntryDisplayNames.ResolveMemberDisplayName(entries, id, memberId)
            : null;
    }

    private static string? ResolveSheetMemberDisplayName(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        MemberId memberId)
    {
        var participation = match.DeclaredParticipations.FirstOrDefault(p => p.Id.Equals(memberId));
        if (participation is null)
        {
            return null;
        }

        var entryId = EntryIdForSide(match, participation.Side);
        return entryId is { } id
            ? EntryDisplayNames.ResolveMemberDisplayName(entries, id, memberId)
            : null;
    }

    private static MatchResultDto? MapResult(MatchResult? result)
    {
        if (result is null)
        {
            return null;
        }

        var shootout = result.PenaltyShootoutScore is { } tab
            ? new MatchScoreDto(tab.HomeGoals, tab.AwayGoals)
            : null;

        return new MatchResultDto(
            result.Type,
            result.Score.HomeGoals,
            result.Score.AwayGoals,
            result.ExtraTimePlayed,
            shootout);
    }

    private static Dictionary<MatchId, FixturePlacement> BuildFixturePlacementIndex(Stage stage)
    {
        var index = new Dictionary<MatchId, FixturePlacement>();
        var sequence = 0;

        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                foreach (var attachment in fixture.Attachments.OrderBy(a => a.LegIndex))
                {
                    index[attachment.MatchId] = new FixturePlacement(
                        sequence++,
                        fixture.Id,
                        round.Id,
                        MatchdayNumber: null,
                        round.Name);
                }
            }
        }

        foreach (var matchday in stage.Matchdays)
        {
            foreach (var fixture in matchday.Fixtures)
            {
                foreach (var attachment in fixture.Attachments.OrderBy(a => a.LegIndex))
                {
                    if (!index.ContainsKey(attachment.MatchId))
                    {
                        index[attachment.MatchId] = new FixturePlacement(
                            sequence++,
                            fixture.Id,
                            RoundId: null,
                            matchday.Number,
                            RoundName: null);
                    }
                }
            }
        }

        return index;
    }

    private static (FixtureId FixtureId, int LegIndex)? FindAttachment(Stage stage, MatchId matchId)
    {
        foreach (var round in stage.Rounds)
        {
            foreach (var fixture in round.Fixtures)
            {
                foreach (var attachment in fixture.Attachments)
                {
                    if (attachment.MatchId.Equals(matchId))
                    {
                        return (fixture.Id, attachment.LegIndex);
                    }
                }
            }
        }

        foreach (var matchday in stage.Matchdays)
        {
            foreach (var fixture in matchday.Fixtures)
            {
                foreach (var attachment in fixture.Attachments)
                {
                    if (attachment.MatchId.Equals(matchId))
                    {
                        return (fixture.Id, attachment.LegIndex);
                    }
                }
            }
        }

        return null;
    }

    private readonly record struct FixturePlacement(
        int Sequence,
        FixtureId? FixtureId,
        RoundId? RoundId,
        int? MatchdayNumber,
        string? RoundName);
}
