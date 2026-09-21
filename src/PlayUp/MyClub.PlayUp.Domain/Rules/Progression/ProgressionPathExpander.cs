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
/// Fixture order = round fixture storage order (stable Expand).
/// </summary>
public static class ProgressionPathExpander
{
    /// <summary>
    /// Materializes intents into paths using the source stage's rounds/fixtures.
    /// </summary>
    /// <param name="intents">Authoring intents.</param>
    /// <param name="rounds">Rounds of the rules-owning stage (fixtures expand per RoundId).</param>
    /// <returns>Derived paths (non-empty when intents non-empty).</returns>
    public static IReadOnlyList<ProgressionPath> Materialize(
        IReadOnlyList<ProgressionIntent> intents,
        IReadOnlyList<Round> rounds)
    {
        ArgumentNullException.ThrowIfNull(intents);
        ArgumentNullException.ThrowIfNull(rounds);

        if (intents.Count == 0)
        {
            throw new DomainException(
                "Progression intents require at least one intent.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

        if (intents.Select(i => i.Order).Distinct().Count() != intents.Count)
        {
            throw new DomainException(
                "Progression intent orders must be unique.",
                RulesErrorCodes.ProgressionRulesInvalid);
        }

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

            if (intent.TargetsPopulation)
            {
                var destination = ProgressionDestination.ForPopulation(intent.DestinationStageId);
                paths.AddRange(round.Fixtures.Select(fixture =>
                    new ProgressionPath(fixture.Id, intent.Outcome, destination.Copy())));
                continue;
            }

            if (intent.DestinationSlotKeys.Count != round.Fixtures.Count)
            {
                throw new DomainException(
                    "Progression place destination slot keys count must equal round fixture count.",
                    RulesErrorCodes.ProgressionRulesInvalid);
            }

            paths.AddRange(round.Fixtures.Select((t, i) => new ProgressionPath(t.Id, intent.Outcome, ProgressionDestination.ForSlot(intent.DestinationStageId, intent.DestinationSlotKeys[i]))));
        }

        return paths;
    }

    /// <summary>
    /// Builds a singleton intent for one path (legacy migration / atomic authoring).
    /// Requires the path's fixture round identity.
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
            path.Destination.SlotKey is null ? [] : [path.Destination.SlotKey]);
}
