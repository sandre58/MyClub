// -----------------------------------------------------------------------
// <copyright file="InboundQualificationSeatProjection.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Read-side qualification seat inference when apply witnesses are missing (legacy data)
/// but standings and destination composition already reflect an applied qual.
/// </summary>
internal static class InboundQualificationSeatProjection
{
    internal static EntryId? TryResolveSeat(
        QualificationPath path,
        Stage sourceStage,
        Stage destination,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        if (sourceStage.Status is not (StageStatus.Completed or StageStatus.Running))
        {
            return null;
        }

        if (matchSummariesByStage is null
            || !matchSummariesByStage.TryGetValue(sourceStage.Id, out var rows)
            || rows.Count == 0)
        {
            return null;
        }

        var matches = rows.Select(ToAttentionSlice).ToArray();
        Standing? overall;
        Dictionary<GroupId, Standing> groups;
        try
        {
            overall = BuildOverall(sourceStage, matches);
            groups = BuildGroups(sourceStage, matches);
        }
        catch (Exception ex) when (ex is ApplicationFailureException or DomainException)
        {
            return null;
        }

        Standing standing;
        try
        {
            standing = ResolveStanding(sourceStage, path, overall, groups, matches);
        }
        catch (Exception)
        {
            return null;
        }

        QualificationInstruction? instruction;
        try
        {
            instruction = QualificationApplier.Apply(path, standing);
        }
        catch (Exception)
        {
            return null;
        }

        return instruction is null ? null : DestinationSatisfaction.EvaluateQualification(destination, sourceStage.Id, path, instruction.EntryId) == DestinationSatisfaction.Kind.Satisfied || ((path.Destination.TargetsForm || path.Destination.TargetsPopulation) && destination.CompositionEntries.Any(entry => entry.EntryId.Equals(instruction.EntryId))) ? instruction.EntryId : null;
    }

    private static MatchAttentionSlice ToAttentionSlice(MatchSummaryRow row) =>
        new(
            row.Id,
            row.StageId,
            row.Status,
            row.HomeEntryId,
            row.AwayEntryId,
            row.Result);

    private static Standing BuildOverall(Stage source, IReadOnlyList<MatchAttentionSlice> matches)
    {
        var participants = matches
            .Where(match => match is { Status: MatchStatus.Finished, Result: not null })
            .SelectMany(match => new[] { match.HomeEntryId, match.AwayEntryId })
            .Distinct()
            .ToArray();
        return CalculateStanding.Execute(
            participants,
            matches,
            RequireStandingRules(source),
            MatchFilter.All,
            CalculateStanding.ToStandingPenalties(source.Penalties));
    }

    private static Dictionary<GroupId, Standing> BuildGroups(
        Stage source,
        IReadOnlyList<MatchAttentionSlice> matches)
    {
        var result = new Dictionary<GroupId, Standing>();
        var rules = RequireStandingRules(source);
        foreach (var group in source.Groups)
        {
            result[group.Id] = CalculateStanding.Execute(
                group.EntryIds,
                matches,
                rules,
                MatchFilter.All,
                CalculateStanding.ToStandingPenalties(source.Penalties));
        }

        return result;
    }

    private static StandingRules RequireStandingRules(Stage source) =>
        source.Regulation.StandingRules
        ?? throw new ApplicationFailureException(
            "Standing rules are required for qualification seat projection.",
            StandingErrorCodes.RulesRequired);

    private static Standing ResolveStanding(
        Stage sourceStage,
        QualificationPath path,
        Standing? overallStanding,
        Dictionary<GroupId, Standing> groupStandings,
        IReadOnlyList<MatchAttentionSlice> matches)
    {
        if (path.Source.Scope == RankingScope.AcrossGroups)
        {
            var position = path.Source.AcrossGroupsPosition
                ?? throw new InvalidOperationException("AcrossGroups position missing.");
            return CrossGroupStandingAssembler.BuildFromAttentionSlices(
                sourceStage.Groups,
                groupStandings,
                position,
                matches,
                RequireStandingRules(sourceStage),
                CalculateStanding.ToStandingPenalties(sourceStage.Penalties));
        }

        if (path.Source.GroupId is null && path.Source.Scope != RankingScope.Group)
        {
            return overallStanding ?? throw new InvalidOperationException("Overall standing missing.");
        }

        var groupId = path.Source.GroupId
                      ?? throw new InvalidOperationException("Group id missing.");
        return groupStandings[groupId];
    }
}
