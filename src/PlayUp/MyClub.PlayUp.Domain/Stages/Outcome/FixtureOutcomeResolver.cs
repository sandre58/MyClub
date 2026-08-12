// -----------------------------------------------------------------------
// <copyright file="FixtureOutcomeResolver.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Pure fixture outcome helper: derives Winner/Loser from TieFormat and confrontation legs.
/// </summary>
/// <remarks>
/// Referential frame is <see cref="EntryId"/> (never SlotA/B, never global Home/Away).
/// Extra time / shootout of confrontation are allowed only on the last leg (otherwise Invalid).
/// TAB without ET is allowed (orthogonal to ExtraTimePlayed).
/// </remarks>
public static class FixtureOutcomeResolver
{
    /// <summary>
    /// Resolves winner and loser for a confrontation.
    /// </summary>
    /// <param name="tieFormat">Round tie format.</param>
    /// <param name="snapshot">Assembled legs.</param>
    /// <returns>The decided fixture outcome.</returns>
    public static FixtureOutcome Resolve(TieFormat tieFormat, FixtureConfrontationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(tieFormat);
        ArgumentNullException.ThrowIfNull(snapshot);

        var legs = ValidateAndOrderLegs(tieFormat, snapshot);
        EnsureFinishedWithScores(legs);
        EnsureNonFinalWithoutConfrontationResolution(tieFormat, legs);
        var (entryA, entryB) = ResolveEntryPair(legs);

        return tieFormat.NumberOfLegs == TieFormat.SingleLeg
            ? ResolveSingleLeg(tieFormat, legs[0])
            : ResolveTwoLegAggregate(tieFormat, legs, entryA, entryB);
    }

    private static FixtureLegSnapshot[] ValidateAndOrderLegs(
        TieFormat tieFormat,
        FixtureConfrontationSnapshot snapshot)
    {
        if (snapshot.Legs.Count != tieFormat.NumberOfLegs)
        {
            throw new DomainException(
                $"Fixture confrontation requires exactly {tieFormat.NumberOfLegs} leg(s).",
                StageErrorCodes.FixtureOutcomeInvalid);
        }

        var ordered = snapshot.Legs.OrderBy(l => l.LegIndex).ToArray();
        for (var i = 0; i < ordered.Length; i++)
        {
            var expected = i + 1;
            if (ordered[i].LegIndex != expected)
            {
                throw new DomainException(
                    $"Fixture confrontation legs must use LegIndex 1..{tieFormat.NumberOfLegs} exactly once.",
                    StageErrorCodes.FixtureOutcomeInvalid);
            }
        }

        return ordered;
    }

    private static void EnsureFinishedWithScores(IReadOnlyList<FixtureLegSnapshot> legs)
    {
        foreach (var leg in legs)
        {
            if (leg.Status != MatchStatus.Finished)
            {
                throw new DomainException(
                    $"Fixture outcome requires finished matches (leg {leg.LegIndex} status was '{leg.Status}').",
                    StageErrorCodes.FixtureOutcomeNotFinished);
            }

            if (leg.Score is null)
            {
                throw new DomainException(
                    $"Fixture outcome requires a score on finished leg {leg.LegIndex}.",
                    StageErrorCodes.FixtureOutcomeInvalid);
            }

            if (leg.HomeEntryId.Equals(leg.AwayEntryId))
            {
                throw new DomainException(
                    "Fixture outcome requires distinct home and away entries on each leg.",
                    StageErrorCodes.FixtureOutcomeInvalid);
            }
        }
    }

    private static void EnsureNonFinalWithoutConfrontationResolution(
        TieFormat tieFormat,
        IReadOnlyList<FixtureLegSnapshot> legs)
    {
        var lastIndex = tieFormat.NumberOfLegs;
        foreach (var leg in legs.Where(l => l.LegIndex < lastIndex))
        {
            if (leg.ExtraTimePlayed || leg.PenaltyShootoutScore is not null)
            {
                throw new DomainException(
                    $"Extra time or penalty shootout is not allowed on non-final leg {leg.LegIndex}.",
                    StageErrorCodes.FixtureOutcomeInvalid);
            }
        }
    }

    private static (EntryId EntryA, EntryId EntryB) ResolveEntryPair(IReadOnlyList<FixtureLegSnapshot> legs)
    {
        var entries = legs
            .SelectMany(l => new[] { l.HomeEntryId, l.AwayEntryId })
            .Distinct()
            .ToArray();

        return entries.Length != 2
            ? throw new DomainException(
                "Fixture confrontation requires exactly two distinct entries across all legs.",
                StageErrorCodes.FixtureOutcomeInvalid)
            : legs.Select(leg => new HashSet<EntryId> { leg.HomeEntryId, leg.AwayEntryId }).Any(pair => !pair.SetEquals(entries))
            ? throw new DomainException(
                "All confrontation legs must share the same two entries.",
                StageErrorCodes.FixtureOutcomeInvalid)
            : ((EntryId EntryA, EntryId EntryB))(entries[0], entries[1]);
    }

    private static FixtureOutcome ResolveSingleLeg(TieFormat tieFormat, FixtureLegSnapshot leg)
    {
        var score = leg.Score!.Value;
        return score.HomeGoals != score.AwayGoals
            ? score.HomeGoals > score.AwayGoals
                ? new FixtureOutcome(leg.HomeEntryId, leg.AwayEntryId)
                : new FixtureOutcome(leg.AwayEntryId, leg.HomeEntryId)
            : ResolveByShootout(tieFormat, leg);
    }

    private static FixtureOutcome ResolveTwoLegAggregate(
        TieFormat tieFormat,
        IReadOnlyList<FixtureLegSnapshot> legs,
        EntryId entryA,
        EntryId entryB)
    {
        var totalA = 0;
        var totalB = 0;
        var awayA = 0;
        var awayB = 0;

        foreach (var leg in legs)
        {
            var score = leg.Score!.Value;
            AddGoals(leg.HomeEntryId, score.HomeGoals, isAway: false, entryA, entryB, ref totalA, ref totalB, ref awayA, ref awayB);
            AddGoals(leg.AwayEntryId, score.AwayGoals, isAway: true, entryA, entryB, ref totalA, ref totalB, ref awayA, ref awayB);
        }

        if (totalA != totalB)
        {
            return totalA > totalB
                ? new FixtureOutcome(entryA, entryB)
                : new FixtureOutcome(entryB, entryA);
        }

        if (tieFormat.AwayGoalsRule is not null && awayA != awayB)
        {
            return awayA > awayB
                ? new FixtureOutcome(entryA, entryB)
                : new FixtureOutcome(entryB, entryA);
        }

        // Aggregate (and away goals when enabled) still tied: ET goals are already in Score totals.
        // Decide only via shootout on the last leg when configured.
        return ResolveByShootout(tieFormat, legs[^1]);
    }

    private static void AddGoals(
        EntryId scorer,
        int goals,
        bool isAway,
        EntryId entryA,
        EntryId entryB,
        ref int totalA,
        ref int totalB,
        ref int awayA,
        ref int awayB)
    {
        if (scorer.Equals(entryA))
        {
            totalA += goals;
            if (isAway)
            {
                awayA += goals;
            }
        }
        else if (scorer.Equals(entryB))
        {
            totalB += goals;
            if (isAway)
            {
                awayB += goals;
            }
        }
    }

    private static FixtureOutcome ResolveByShootout(TieFormat tieFormat, FixtureLegSnapshot leg)
    {
        if (tieFormat.PenaltyShootoutRule is not null)
        {
            return leg.PenaltyShootoutScore is not { } shootout
                ? throw new DomainException(
                    "Fixture outcome is undecided when the score is a draw.",
                    StageErrorCodes.FixtureOutcomeUndecided)
                : shootout.HomeGoals == shootout.AwayGoals
                    ? throw new DomainException(
                        "Fixture outcome requires a decisive penalty shootout score.",
                        StageErrorCodes.FixtureOutcomeInvalid)
                    : shootout.HomeGoals > shootout.AwayGoals
                        ? new FixtureOutcome(leg.HomeEntryId, leg.AwayEntryId)
                        : new FixtureOutcome(leg.AwayEntryId, leg.HomeEntryId);
        }

        if (leg.PenaltyShootoutScore is not null)
        {
            throw new DomainException(
                "Penalty shootout score is present but TieFormat does not allow a shootout.",
                StageErrorCodes.FixtureOutcomeInvalid);
        }

        throw new DomainException(
            "Fixture outcome is undecided when the score is a draw.",
            StageErrorCodes.FixtureOutcomeUndecided);
    }
}
