// -----------------------------------------------------------------------
// <copyright file="StructureReadyConsumerFeedGate.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Reads;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// G-OPS2: refuse operational mutations when Ready downstream consumers would lose a satisfied feed
/// contract (promised inbound volume vs placed composition).
/// </summary>
internal static class StructureReadyConsumerFeedGate
{
    internal static void EnsureQualificationReplaceAllowed(
        Stage upstream,
        QualificationRules? afterRules,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(upstream);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var beforeRules = upstream.Regulation.QualificationRules;
        var destinationIds = EnumerateQualificationDestinations(beforeRules)
            .Union(EnumerateQualificationDestinations(afterRules))
            .ToHashSet();

        EnsurePromisedInboundChange(
            competitionStages,
            destinationIds,
            destinationId => InboundQualificationVolume(destinationId, beforeRules),
            destinationId => InboundQualificationVolume(destinationId, afterRules),
            "Qualification rules");
    }

    internal static void EnsureProgressionReplaceAllowed(
        Stage upstream,
        ProgressionRules? afterRules,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(upstream);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var beforeRules = upstream.Regulation.ProgressionRules;
        var destinationIds = EnumerateProgressionDestinations(beforeRules)
            .Union(EnumerateProgressionDestinations(afterRules))
            .ToHashSet();

        EnsurePromisedInboundChange(
            competitionStages,
            destinationIds,
            destinationId => InboundProgressionVolume(destinationId, beforeRules),
            destinationId => InboundProgressionVolume(destinationId, afterRules),
            "Progression rules");
    }

    /// <summary>
    /// D1 for affectation: qual/prog rules unchanged; shrinking source population caps promised feed to Ready consumers.
    /// </summary>
    internal static void EnsureAffectationReplaceAllowed(
        Stage upstream,
        IReadOnlyList<EntryId> afterEntryIds,
        IReadOnlyList<Stage> competitionStages)
    {
        ArgumentNullException.ThrowIfNull(upstream);
        ArgumentNullException.ThrowIfNull(afterEntryIds);
        ArgumentNullException.ThrowIfNull(competitionStages);

        var destinationIds = EnumerateOutboundConsumerDestinations(upstream).ToHashSet();
        if (destinationIds.Count == 0)
        {
            return;
        }

        var beforePopulation = ResolveSourcePopulation(upstream, upstream.AffectationAuthoring.Count);
        var afterPopulation = ResolveSourcePopulationAfterAffectationReplace(upstream, afterEntryIds);

        EnsurePromisedInboundChange(
            competitionStages,
            destinationIds,
            destinationId => PromisedInboundFromSource(upstream, destinationId, beforePopulation),
            destinationId => PromisedInboundFromSource(upstream, destinationId, afterPopulation),
            "Affectation authoring");
    }

    private static void EnsurePromisedInboundChange(
        IReadOnlyList<Stage> competitionStages,
        HashSet<StageId> destinationIds,
        Func<StageId, int> promisedBefore,
        Func<StageId, int> promisedAfter,
        string operationLabel)
    {
        var broken = CollectBrokenReadyConsumers(
            destinationIds,
            promisedBefore,
            promisedAfter,
            competitionStages);

        if (broken.Count == 0)
        {
            return;
        }

        var consumerIds = broken.Select(stage => stage.Id.Value.ToString()).ToArray();
        throw new ApplicationFailureException(
            $"{operationLabel} cannot be replaced: Ready downstream consumer feed would break ({string.Join(", ", broken.Select(s => s.Name.Value))}).",
            ApplicationErrorCodes.ReadyConsumerWouldBreak,
            consumerIds);
    }

    private static List<Stage> CollectBrokenReadyConsumers(
        HashSet<StageId> destinationIds,
        Func<StageId, int> promisedBefore,
        Func<StageId, int> promisedAfter,
        IReadOnlyList<Stage> competitionStages)
    {
        var broken = new List<Stage>();
        foreach (var destinationId in destinationIds)
        {
            var consumer = competitionStages.FirstOrDefault(stage => stage.Id.Equals(destinationId));
            if (consumer is null || consumer.Status is not StageStatus.Ready)
            {
                continue;
            }

            var before = promisedBefore(destinationId);
            var after = promisedAfter(destinationId);
            if (before == after)
            {
                continue;
            }

            var placedCount = consumer.CompositionEntries.Count;
            if (placedCount == 0)
            {
                continue;
            }

            if (after == 0 && before > 0)
            {
                broken.Add(consumer);
                continue;
            }

            if (placedCount > after)
            {
                broken.Add(consumer);
            }
        }

        return broken;
    }

    private static IEnumerable<StageId> EnumerateOutboundConsumerDestinations(Stage upstream)
    {
        foreach (var id in EnumerateQualificationDestinations(upstream.Regulation.QualificationRules))
        {
            yield return id;
        }

        foreach (var id in EnumerateProgressionDestinations(upstream.Regulation.ProgressionRules))
        {
            yield return id;
        }
    }

    private static int PromisedInboundFromSource(Stage upstream, StageId destinationId, int sourcePopulation)
    {
        var configured = InboundConfiguredFromSource(upstream, destinationId);
        return configured == 0 ? 0 : Math.Min(configured, sourcePopulation);
    }

    private static int InboundConfiguredFromSource(Stage upstream, StageId destinationId)
    {
        var volume = InboundQualificationVolume(destinationId, upstream.Regulation.QualificationRules);
        volume += InboundProgressionVolume(destinationId, upstream.Regulation.ProgressionRules);
        return volume;
    }

    private static int ResolveSourcePopulation(Stage upstream, int affectationCount) =>
        Math.Max(upstream.CompositionEntries.Count, affectationCount);

    private static int ResolveSourcePopulationAfterAffectationReplace(
        Stage upstream,
        IReadOnlyList<EntryId> afterEntryIds)
    {
        var previous = upstream.AffectationAuthoring.Select(entry => entry.EntryId).ToHashSet();
        var next = new HashSet<EntryId>();
        foreach (var entryId in afterEntryIds)
        {
            next.Add(entryId);
        }

        var composition = upstream.CompositionEntries.Select(entry => entry.EntryId).ToHashSet();
        foreach (var removed in previous.Where(id => !next.Contains(id)))
        {
            composition.Remove(removed);
        }

        foreach (var added in next.Where(id => !previous.Contains(id)))
        {
            composition.Add(added);
        }

        return Math.Max(composition.Count, next.Count);
    }

    private static IEnumerable<StageId> EnumerateQualificationDestinations(QualificationRules? rules) =>
        rules?.Paths.Select(path => path.Destination.StageId) ?? [];

    private static IEnumerable<StageId> EnumerateProgressionDestinations(ProgressionRules? rules) =>
        rules?.Paths.Select(path => path.Destination.StageId) ?? [];

    private static int InboundQualificationVolume(StageId destinationStageId, QualificationRules? rules)
    {
        if (rules is null)
        {
            return 0;
        }

        return rules.Paths
            .Where(path => path.Destination.StageId.Equals(destinationStageId))
            .Sum(StructurePopulationVolume.QualificationPathVolume);
    }

    private static int InboundProgressionVolume(StageId destinationStageId, ProgressionRules? rules)
    {
        if (rules is null)
        {
            return 0;
        }

        return rules.Paths.Count(path => path.Destination.StageId.Equals(destinationStageId));
    }
}
