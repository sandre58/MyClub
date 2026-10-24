// -----------------------------------------------------------------------
// <copyright file="PhaseRosterPresentationClassifier.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Application.Stages;
using MyClub.PlayUp.Application.Standings;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Qualification;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;
using MyClub.PlayUp.Domain.Standings;

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Classifies roster gaps for downstream presentation (A3) without altering lifecycle or materialize gates.
/// </summary>
public static class PhaseRosterPresentationClassifier
{
    internal enum InboundQualSeatProductState
    {
        NotYetProducible,
        ProducibleButNotConsumed,
        Conflict
    }

    public static PhaseRosterPresentationKind Classify(
        Competition competition,
        Stage destination,
        IReadOnlyList<Stage> competitionStages,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage = null)
    {
        ArgumentNullException.ThrowIfNull(competition);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(competitionStages);

        if (ChampionshipSwissPhasePlaces.IsRosterComplete(competition, destination, competitionStages))
        {
            return PhaseRosterPresentationKind.Complete;
        }

        var placesN = ChampionshipSwissPhasePlaces.ResolvePlacesN(
            competition,
            destination,
            competitionStages);
        if (placesN is null or <= 0)
        {
            return PhaseRosterPresentationKind.CompositionIncomplete;
        }

        var rosterCount = ChampionshipSwissPhasePlaces
            .ResolveCompositionRoster(competition, destination)
            .Count;
        var rosterGap = placesN.Value - rosterCount;
        if (rosterGap <= 0)
        {
            return PhaseRosterPresentationKind.Complete;
        }

        if (StructureStageInboundTopology.IsRootComposition(destination, competitionStages))
        {
            return PhaseRosterPresentationKind.CompositionIncomplete;
        }

        var inbound = StructureInboundCompositionAssembler.Assemble(
            competition,
            destination,
            competitionStages,
            matchSummariesByStage);
        if (inbound is null)
        {
            return PhaseRosterPresentationKind.CompositionIncomplete;
        }

        var notYetProducibleUnresolved = 0;
        foreach (var rule in inbound.Rules)
        {
            var source = competitionStages.FirstOrDefault(stage => stage.Id.Value == rule.SourceStageId);
            if (source is null)
            {
                return PhaseRosterPresentationKind.CompositionIncomplete;
            }

            foreach (var seat in rule.Seats)
            {
                if (seat.EntryId is not null)
                {
                    continue;
                }

                var state = rule.QualificationPath is not null
                    ? FindQualificationPath(source, rule.QualificationPath) is { } qualPath
                        ? ClassifyUnresolvedQualSeat(
                            qualPath,
                            source,
                            destination,
                            matchSummariesByStage)
                        : InboundQualSeatProductState.ProducibleButNotConsumed
                    : rule.ProgressionPath is not null
                        ? FindProgressionPath(source, rule.ProgressionPath) is { } progPath
                            ? ClassifyUnresolvedProgressionSeat(
                                progPath,
                                source,
                                destination,
                                matchSummariesByStage)
                            : InboundQualSeatProductState.ProducibleButNotConsumed
                        : InboundQualSeatProductState.ProducibleButNotConsumed;

                switch (state)
                {
                    case InboundQualSeatProductState.NotYetProducible:
                        notYetProducibleUnresolved++;
                        break;
                    case InboundQualSeatProductState.ProducibleButNotConsumed:
                    case InboundQualSeatProductState.Conflict:
                        return PhaseRosterPresentationKind.CompositionIncomplete;
                }
            }
        }

        return notYetProducibleUnresolved == rosterGap
            ? PhaseRosterPresentationKind.AwaitingInboundQualification
            : PhaseRosterPresentationKind.CompositionIncomplete;
    }

    internal static InboundQualSeatProductState ClassifyUnresolvedQualSeat(
        QualificationPath path,
        Stage sourceStage,
        Stage destination,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        if (path.Destination is { TargetsForm: false, TargetsPopulation: false })
        {
            return InboundQualificationSeatProjection.TryResolveSeat(path, sourceStage, destination, matchSummariesByStage) is not null ? InboundQualSeatProductState.ProducibleButNotConsumed : sourceStage.Status is not (StageStatus.Running or StageStatus.Completed) || !TryResolveQualificationInstruction(sourceStage, path, matchSummariesByStage, out var instruction) || instruction is null ? InboundQualSeatProductState.NotYetProducible : MapSatisfaction(DestinationSatisfaction.EvaluateQualification(destination, sourceStage.Id, path, instruction.EntryId));
        }

        var fingerprint = FormPathResolutionKey.FromQualification(sourceStage.Id, path);
        var witness = destination.FormPathResolutions
            .FirstOrDefault(resolution => resolution.PathFingerprint == fingerprint);
        return witness is not null ? MapSatisfaction(DestinationSatisfaction.EvaluateQualification(destination, sourceStage.Id, path, witness.EntryId)) : InboundQualificationSeatProjection.TryResolveSeat(path, sourceStage, destination, matchSummariesByStage) is not null ? InboundQualSeatProductState.ProducibleButNotConsumed : sourceStage.Status is not (StageStatus.Running or StageStatus.Completed) || !TryResolveQualificationInstruction(sourceStage, path, matchSummariesByStage, out var instruction1) || instruction1 is null ? InboundQualSeatProductState.NotYetProducible : MapSatisfaction(DestinationSatisfaction.EvaluateQualification(destination, sourceStage.Id, path, instruction1.EntryId));
    }

    private static InboundQualSeatProductState ClassifyUnresolvedProgressionSeat(
        ProgressionPath path,
        Stage sourceStage,
        Stage destination,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage)
    {
        var entryId = InboundFeedSeatResolver.ResolveProgressionEntry(
            path,
            destination,
            sourceStage,
            matchSummariesByStage);
        return entryId is not null
            ? MapSatisfaction(
                DestinationSatisfaction.EvaluateProgression(
                    destination,
                    sourceStage.Id,
                    path,
                    entryId.Value))
            : InboundQualSeatProductState.NotYetProducible;
    }

    private static InboundQualSeatProductState MapSatisfaction(DestinationSatisfaction.Kind kind) =>
        kind switch
        {
            DestinationSatisfaction.Kind.Satisfied => InboundQualSeatProductState.ProducibleButNotConsumed,
            DestinationSatisfaction.Kind.Conflict => InboundQualSeatProductState.Conflict,
            _ => InboundQualSeatProductState.ProducibleButNotConsumed
        };

    private static bool TryResolveQualificationInstruction(
        Stage sourceStage,
        QualificationPath path,
        IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>>? matchSummariesByStage,
        out QualificationInstruction? instruction)
    {
        instruction = null;
        if (matchSummariesByStage is null
            || !matchSummariesByStage.TryGetValue(sourceStage.Id, out var rows)
            || rows.Count == 0)
        {
            return false;
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
            return false;
        }

        Standing standing;
        try
        {
            standing = ResolveStanding(sourceStage, path, overall, groups, matches);
        }
        catch (Exception)
        {
            return false;
        }

        try
        {
            instruction = QualificationApplier.Apply(path, standing);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
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

    private static QualificationPath? FindQualificationPath(
        Stage source,
        StructureInboundQualificationPathDto pathDto)
    {
        var rules = source.Regulation.QualificationRules;
        if (rules is null)
        {
            return null;
        }

        foreach (var path in rules.Paths)
        {
            if (path.Order != pathDto.Order)
            {
                continue;
            }

            if (pathDto.GroupId is { } groupId
                && path.Source.GroupId?.Value != groupId)
            {
                continue;
            }

            return path;
        }

        return null;
    }

    private static ProgressionPath? FindProgressionPath(
        Stage source,
        StructureInboundProgressionPathDto pathDto)
    {
        var rules = source.Regulation.ProgressionRules;

        return rules?.Paths.FirstOrDefault(path =>
            string.Equals(path.SourcePairKey, pathDto.SourcePairKey, StringComparison.Ordinal)
            && path.Outcome == pathDto.Outcome);
    }
}
