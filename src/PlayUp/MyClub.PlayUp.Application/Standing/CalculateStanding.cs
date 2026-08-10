// -----------------------------------------------------------------------
// <copyright file="CalculateStanding.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standing;
using MatchAggregate = MyClub.PlayUp.Domain.Match.Match;
using StandingView = MyClub.PlayUp.Domain.Standing.Standing;

namespace MyClub.PlayUp.Application.Standing;

/// <summary>
/// Application helper: assemble finished matches and calculate a standing view.
/// </summary>
public static class CalculateStanding
{
    /// <summary>
    /// Calculates a standing for the given participants and matches.
    /// </summary>
    /// <param name="participants">Ranking universe entries.</param>
    /// <param name="matches">Already-loaded matches (non-finished ignored).</param>
    /// <param name="rules">Standing rules.</param>
    /// <param name="filter">Match filter (default all).</param>
    /// <returns>Calculated standing view.</returns>
    public static StandingView Execute(
        IReadOnlyList<EntryId> participants,
        IEnumerable<MatchAggregate> matches,
        StandingRules rules,
        MatchFilter filter = MatchFilter.All)
    {
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(rules);

        var snapshots = StandingMatchAssembler.Assemble(matches);
        return StandingCalculator.Calculate(participants, snapshots, rules, filter);
    }
}
