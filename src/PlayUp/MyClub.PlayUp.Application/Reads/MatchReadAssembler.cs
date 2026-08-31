// -----------------------------------------------------------------------
// <copyright file="MatchReadAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

        return
        [
            .. matches
                .OrderBy(match => placement.TryGetValue(match.Id, out var info) ? info.Sequence : int.MaxValue)
                .ThenBy(match => match.Id.Value)
                .Select(match =>
                {
                    ResolveCalendarPlacement(stage, match.Id, out var scheduledAt, out var resourceId);
                    placement.TryGetValue(match.Id, out var info);

                    return new MatchSummaryDto(
                        match.Id.Value,
                        match.StageId.Value,
                        match.Status,
                        EntryDisplayNames.ToSide(entries, match.HomeEntryId),
                        EntryDisplayNames.ToSide(entries, match.AwayEntryId),
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
                resourceId);
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
                resourceId);
        }

        fixtureId = found.FixtureId.Value;
        legIndex = found.LegIndex;

        return BuildDetail(
            match,
            entries,
            fixtureId,
            legIndex,
            scheduledAt,
            resourceId);
    }

    /// <summary>
    /// Builds Consultation-compatible context label from projected summary fields.
    /// Matchday labels stay FR for wire stability of <see cref="ConsultationResultDto"/>.
    /// </summary>
    public static string? ConsultationContextLabel(MatchSummaryDto summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return summary.RoundName is { Length: > 0 }
            ? summary.RoundName
            : summary.MatchdayNumber is { } number
            ? $"Journée {number}"
            : null;
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
        Guid? resourceId) =>
        new(
            match.Id.Value,
            match.CompetitionId.Value,
            match.StageId.Value,
            match.Status,
            EntryDisplayNames.ToSide(entries, match.HomeEntryId),
            EntryDisplayNames.ToSide(entries, match.AwayEntryId),
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

    private static EntryId EntryIdForSide(Match match, Side side) =>
        side == Side.Home ? match.HomeEntryId : match.AwayEntryId;

    private static string? ResolveSideMemberDisplayName(
        Match match,
        IReadOnlyDictionary<EntryId, CompetitionEntry> entries,
        Side side,
        MemberId memberId) =>
        EntryDisplayNames.ResolveMemberDisplayName(entries, EntryIdForSide(match, side), memberId);

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

        return EntryDisplayNames.ResolveMemberDisplayName(
            entries,
            EntryIdForSide(match, participation.Side),
            memberId);
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
