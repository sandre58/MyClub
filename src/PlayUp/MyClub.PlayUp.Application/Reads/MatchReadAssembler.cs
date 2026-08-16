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

        var names = EntryDisplayNames.ToMap(competition);
        var placement = BuildFixturePlacementIndex(stage);

        return
        [
            .. matches
                .OrderBy(match => placement.TryGetValue(match.Id, out var info) ? info.Sequence : int.MaxValue)
                .ThenBy(match => match.Id.Value)
                .Select(match =>
                {
                    ResolveCalendarPlacement(stage, match.Id, out var scheduledAt, out var resourceId);

                    if (!placement.TryGetValue(match.Id, out var info))
                    {
                        return new MatchSummaryDto(
                            match.Id.Value,
                            match.StageId.Value,
                            match.Status,
                            new EntrySideDto(match.HomeEntryId.Value,
                                EntryDisplayNames.Resolve(names, match.HomeEntryId)),
                            new EntrySideDto(match.AwayEntryId.Value,
                                EntryDisplayNames.Resolve(names, match.AwayEntryId)),
                            MapScore(match.Result),
                            FixtureId: null,
                            RoundId: null,
                            scheduledAt,
                            resourceId);
                    }

                    return new MatchSummaryDto(
                        match.Id.Value,
                        match.StageId.Value,
                        match.Status,
                        new EntrySideDto(match.HomeEntryId.Value, EntryDisplayNames.Resolve(names, match.HomeEntryId)),
                        new EntrySideDto(match.AwayEntryId.Value, EntryDisplayNames.Resolve(names, match.AwayEntryId)),
                        MapScore(match.Result),
                        info.FixtureId.Value,
                        info.RoundId?.Value,
                        scheduledAt,
                        resourceId);
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

        var names = EntryDisplayNames.ToMap(competition);
        DateTimeOffset? scheduledAt = null;
        Guid? resourceId = null;
        if (stage is not null)
        {
            ResolveCalendarPlacement(stage, match.Id, out scheduledAt, out resourceId);
        }

        if (stage is null)
        {
            return new MatchDetailDto(
                match.Id.Value,
                match.CompetitionId.Value,
                match.StageId.Value,
                match.Status,
                new EntrySideDto(match.HomeEntryId.Value, EntryDisplayNames.Resolve(names, match.HomeEntryId)),
                new EntrySideDto(match.AwayEntryId.Value, EntryDisplayNames.Resolve(names, match.AwayEntryId)),
                MapResult(match.Result),
                FixtureId: null,
                LegIndex: null,
                scheduledAt,
                resourceId);
        }

        var attachment = FindAttachment(stage, match.Id);
        if (attachment is not { } found)
        {
            return new MatchDetailDto(
                match.Id.Value,
                match.CompetitionId.Value,
                match.StageId.Value,
                match.Status,
                new EntrySideDto(match.HomeEntryId.Value, EntryDisplayNames.Resolve(names, match.HomeEntryId)),
                new EntrySideDto(match.AwayEntryId.Value, EntryDisplayNames.Resolve(names, match.AwayEntryId)),
                MapResult(match.Result),
                FixtureId: null,
                LegIndex: null,
                scheduledAt,
                resourceId);
        }

        return new MatchDetailDto(
            match.Id.Value,
            match.CompetitionId.Value,
            match.StageId.Value,
            match.Status,
            new EntrySideDto(match.HomeEntryId.Value, EntryDisplayNames.Resolve(names, match.HomeEntryId)),
            new EntrySideDto(match.AwayEntryId.Value, EntryDisplayNames.Resolve(names, match.AwayEntryId)),
            MapResult(match.Result),
            found.FixtureId.Value,
            found.LegIndex,
            scheduledAt,
            resourceId);
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
                        round.Id);
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
                            RoundId: null);
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

    private readonly record struct FixturePlacement(int Sequence, FixtureId FixtureId, RoundId? RoundId);
}
