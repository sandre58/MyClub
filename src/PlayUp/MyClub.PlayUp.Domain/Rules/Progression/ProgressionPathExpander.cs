// -----------------------------------------------------------------------
// <copyright file="ProgressionPathExpander.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Expands <see cref="ProgressionIntent"/> into atomic <see cref="ProgressionPath"/> (V3).
/// Cup V1: Expand on <see cref="BracketPair"/> order (PairKey) — fixtures not required at Save.
/// Non-Cup (no BracketPairs): Expand on round fixture order (interim SourcePairKey = fixture Guid N).
/// </summary>
public static class ProgressionPathExpander
{
    /// <summary>
    /// Materializes intents into paths using the source stage form.
    /// </summary>
    /// <param name="intents">Authoring intents.</param>
    /// <param name="rounds">Rounds of the rules-owning stage.</param>
    /// <param name="bracketPairs">Cup structural pairs (empty for non-Cup).</param>
    /// <returns>Derived paths (non-empty when intents non-empty).</returns>
    public static IReadOnlyList<ProgressionPath> Materialize(
        IReadOnlyList<ProgressionIntent> intents,
        IReadOnlyList<Round> rounds,
        IReadOnlyList<BracketPair> bracketPairs)
    {
        ArgumentNullException.ThrowIfNull(intents);
        ArgumentNullException.ThrowIfNull(rounds);
        ArgumentNullException.ThrowIfNull(bracketPairs);

        return intents.Count == 0
            ? throw new DomainException(
                "Progression intents require at least one intent.",
                RulesErrorCodes.ProgressionRulesInvalid)
            : intents.Select(i => i.Order).Distinct().Count() != intents.Count
            ? throw new DomainException(
                "Progression intent orders must be unique.",
                RulesErrorCodes.ProgressionRulesInvalid)
            : bracketPairs.Count > 0
            ? MaterializeFromPairs(intents, rounds, bracketPairs)
            : MaterializeFromFixtures(intents, rounds);
    }

    /// <summary>
    /// Builds a singleton intent for one path (legacy migration / atomic authoring).
    /// </summary>
    public static ProgressionIntent ToSingletonIntent(
        ProgressionPath path,
        RoundId roundId,
        IntentId? id = null) =>
        new(
            id ?? IntentId.New(),
            order: 1,
            roundId,
            path.Outcome,
            path.Destination.StageId,
            path.Destination.TargetsSlot ? [path.Destination.SlotKey!] : null,
            path.Destination.TargetsGroup ? [path.Destination.GroupId!.Value] : null,
            destinationForm: path.Destination.TargetsForm);

    private static List<ProgressionPath> MaterializeFromPairs(
        IReadOnlyList<ProgressionIntent> intents,
        IReadOnlyList<Round> rounds,
        IReadOnlyList<BracketPair> bracketPairs)
    {
        var roundById = rounds.ToDictionary(r => r.Id);
        var orderedPairs = bracketPairs
            .OrderBy(p => p.PairKey, StringComparer.Ordinal)
            .ToArray();
        var paths = new List<ProgressionPath>();

        foreach (var intent in intents.OrderBy(i => i.Order))
        {
            if (!roundById.TryGetValue(intent.RoundId, out _))
            {
                throw new DomainException(
                    $"Progression intent round '{intent.RoundId}' was not found on the source stage.",
                    RulesErrorCodes.ProgressionRulesInvalid);
            }

            if (intent.Outcome == ProgressionOutcome.Winner
                && !ProgressionChampionshipPath.IsChampionshipTerminal(rounds, intent.RoundId))
            {
                throw new DomainException(
                    "Progression Winner intent must use the championship-path terminal round.",
                    RulesErrorCodes.ProgressionRulesInvalid);
            }

            paths.AddRange(ExpandZip(intent, [.. orderedPairs.Select(p => p.PairKey)]));
        }

        return paths;
    }

    private static List<ProgressionPath> MaterializeFromFixtures(
        IReadOnlyList<ProgressionIntent> intents,
        IReadOnlyList<Round> rounds)
    {
        var roundById = rounds.ToDictionary(r => r.Id);
        var paths = new List<ProgressionPath>();

        foreach (var intent in intents.OrderBy(i => i.Order))
        {
            if (!roundById.TryGetValue(intent.RoundId, out var round))
            {
                throw new DomainException(
                    $"Progression intent round '{intent.RoundId}' was not found on the source stage.",
                    RulesErrorCodes.ProgressionRulesInvalid);
            }

            if (round.Fixtures.Count == 0)
            {
                throw new DomainException(
                    $"Progression intent round '{intent.RoundId}' has no fixtures to expand.",
                    RulesErrorCodes.ProgressionRulesInvalid);
            }

            if (intent.Outcome == ProgressionOutcome.Winner
                && !ProgressionChampionshipPath.IsChampionshipTerminal(rounds, intent.RoundId))
            {
                throw new DomainException(
                    "Progression Winner intent must use the championship-path terminal round.",
                    RulesErrorCodes.ProgressionRulesInvalid);
            }

            var keys = round.Fixtures
                .Select(f => f.BracketPairKey ?? f.Id.Value.ToString("N"))
                .ToArray();
            paths.AddRange(ExpandZip(intent, keys));
        }

        return paths;
    }

    private static IEnumerable<ProgressionPath> ExpandZip(
        ProgressionIntent intent,
        string[] sourceKeys)
    {
        if (intent.TargetsPopulation)
        {
            var destination = ProgressionDestination.ForPopulation(intent.DestinationStageId);
            return sourceKeys.Select(key =>
                new ProgressionPath(key, intent.Outcome, destination.Copy()));
        }

        if (!intent.TargetsForm)
        {
            return intent.TargetsGroup
                ? intent.DestinationGroupIds.Count != sourceKeys.Length
                    ? throw new DomainException(
                        "Progression place destination group ids count must equal expand source count.",
                        RulesErrorCodes.ProgressionRulesInvalid)
                    : sourceKeys.Select((key, i) => new ProgressionPath(
                        key,
                        intent.Outcome,
                        ProgressionDestination.ForGroup(
                            intent.DestinationStageId,
                            intent.DestinationGroupIds[i])))
                : intent.DestinationSlotKeys.Count != sourceKeys.Length
                    ? throw new DomainException(
                        "Progression place destination slot keys count must equal expand source count.",
                        RulesErrorCodes.ProgressionRulesInvalid)
                    : sourceKeys.Select((key, i) => new ProgressionPath(
                        key,
                        intent.Outcome,
                        ProgressionDestination.ForSlot(intent.DestinationStageId, intent.DestinationSlotKeys[i])));
        }

        {
            var destination = ProgressionDestination.ForForm(intent.DestinationStageId);
            return sourceKeys.Select(key =>
                new ProgressionPath(key, intent.Outcome, destination.Copy()));
        }
    }
}
