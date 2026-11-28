// -----------------------------------------------------------------------
// <copyright file="TwoLegAggregateStillLevel.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Matches;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Result of evaluating two-legged play scores (aggregate and optional away goals).
/// </summary>
public enum TwoLegAggregatePlayDecision
{
    /// <summary>Entry A leads on aggregate or away goals.</summary>
    EntryAWins,

    /// <summary>Entry B leads on aggregate or away goals.</summary>
    EntryBWins,

    /// <summary>Aggregate and away goals (when active) leave the confrontation level.</summary>
    StillLevel
}

/// <summary>
/// Pure helper: aggregate and away-goals evaluation shared by outcome resolution and match entry eligibility.
/// </summary>
public static class TwoLegAggregateStillLevel
{
    /// <summary>
    /// Returns whether aggregate totals are tied and away goals (when active) do not break the tie.
    /// </summary>
    public static bool AfterPlayScores(
        TieFormat tieFormat,
        EntryId entryA,
        EntryId entryB,
        IReadOnlyList<FixtureLegSnapshot> legs) =>
        Evaluate(tieFormat, entryA, entryB, legs) == TwoLegAggregatePlayDecision.StillLevel;

    /// <summary>
    /// Evaluates play scores across both legs.
    /// </summary>
    public static TwoLegAggregatePlayDecision Evaluate(
        TieFormat tieFormat,
        EntryId entryA,
        EntryId entryB,
        IReadOnlyList<FixtureLegSnapshot> legs)
    {
        ArgumentNullException.ThrowIfNull(tieFormat);
        ArgumentNullException.ThrowIfNull(legs);

        var totalA = 0;
        var totalB = 0;
        var awayA = 0;
        var awayB = 0;

        foreach (var leg in legs)
        {
            var score = leg.Score!.Value;
            AddGoals(leg.HomeEntryId!.Value, score.HomeGoals, isAway: false, entryA, entryB, ref totalA, ref totalB, ref awayA, ref awayB);
            AddGoals(leg.AwayEntryId!.Value, score.AwayGoals, isAway: true, entryA, entryB, ref totalA, ref totalB, ref awayA, ref awayB);
        }

        if (totalA > totalB)
        {
            return TwoLegAggregatePlayDecision.EntryAWins;
        }

        if (totalB > totalA)
        {
            return TwoLegAggregatePlayDecision.EntryBWins;
        }

        if (tieFormat.AwayGoalsRule is not null && awayA != awayB)
        {
            return awayA > awayB
                ? TwoLegAggregatePlayDecision.EntryAWins
                : TwoLegAggregatePlayDecision.EntryBWins;
        }

        return TwoLegAggregatePlayDecision.StillLevel;
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
}
