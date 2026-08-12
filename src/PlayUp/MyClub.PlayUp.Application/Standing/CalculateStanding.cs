// -----------------------------------------------------------------------
// <copyright file="CalculateStanding.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Standing;
using MatchAggregate = MyClub.PlayUp.Domain.Match.Match;
using PenaltyEntity = MyClub.PlayUp.Domain.Stage.Penalty;
using StandingView = MyClub.PlayUp.Domain.Standing.Standing;

namespace MyClub.PlayUp.Application.Standing;

/// <summary>
/// Application helper: assemble finished matches and calculate a standing view.
/// Deduction of penalty points is performed only by <see cref="StandingCalculator"/> —
/// Host/Application must supply stage penalties and must not subtract points themselves.
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
    /// <param name="penalties">
    /// Optional standing penalty snapshots (typically mapped from <c>Stage.Penalties</c>).
    /// </param>
    /// <returns>Calculated standing view.</returns>
    public static StandingView Execute(
        IReadOnlyList<EntryId> participants,
        IEnumerable<MatchAggregate> matches,
        StandingRules rules,
        MatchFilter filter = MatchFilter.All,
        IReadOnlyList<StandingPenalty>? penalties = null)
    {
        ArgumentNullException.ThrowIfNull(participants);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(rules);

        var snapshots = StandingMatchAssembler.Assemble(matches);
        return StandingCalculator.Calculate(participants, snapshots, rules, filter, penalties);
    }

    /// <summary>
    /// Maps stage penalty entities to calculation snapshots (pure mapping; no deduction).
    /// </summary>
    public static IReadOnlyList<StandingPenalty> ToStandingPenalties(IEnumerable<PenaltyEntity> penalties)
    {
        ArgumentNullException.ThrowIfNull(penalties);
        return [..penalties.Select(p => new StandingPenalty(p.EntryId, p.PointsDeducted))];
    }
}
