// -----------------------------------------------------------------------
// <copyright file="CrossGroupStandingAssembler.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Standing;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stage;
using MyClub.PlayUp.Domain.Standing;
using MatchAggregate = MyClub.PlayUp.Domain.Match.Match;
using StandingView = MyClub.PlayUp.Domain.Standing.Standing;

namespace MyClub.PlayUp.Application.Stage;

/// <summary>
/// Builds a derived standing from the same position across every group on a stage.
/// </summary>
/// <remarks>
/// Missing <c>EntryAt(P)</c> is skipped. Every stage group must have a standing.
/// Duplicate candidates and an empty candidate set fail explicitly.
/// Match statistics reuse <see cref="CalculateStanding"/> with stage standing rules
/// (opponents outside the candidate set still count).
/// Supply <c>Stage.Penalties</c> (mapped) so deductions stay inside <see cref="StandingCalculator"/>;
/// Host/Application must not subtract points themselves.
/// </remarks>
public static class CrossGroupStandingAssembler
{
    /// <summary>
    /// Extracts position <paramref name="position"/> from each group standing and ranks those candidates.
    /// </summary>
    /// <param name="groups">Source stage groups (iteration order is stable; ranking does not depend on it).</param>
    /// <param name="groupStandings">Per-group standings keyed by <see cref="GroupId"/>.</param>
    /// <param name="position">1-based standing position to extract from each group.</param>
    /// <param name="matches">Stage matches used to recalculate candidate statistics.</param>
    /// <param name="standingRules">Stage standing rules.</param>
    /// <param name="penalties">
    /// Optional penalties from the source stage (typically <c>CalculateStanding.ToStandingPenalties(stage.Penalties)</c>).
    /// </param>
    /// <returns>Derived standing of across-groups candidates.</returns>
    public static StandingView Build(
        IReadOnlyList<Group> groups,
        IReadOnlyDictionary<GroupId, StandingView> groupStandings,
        int position,
        IReadOnlyList<MatchAggregate> matches,
        StandingRules standingRules,
        IReadOnlyList<StandingPenalty>? penalties = null)
    {
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(groupStandings);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentNullException.ThrowIfNull(standingRules);

        if (position < 1)
        {
            throw new ApplicationFailureException(
                "Across-groups position must be at least 1.",
                ApplicationErrorCodes.QualificationCandidatesEmpty);
        }

        var candidates = new List<EntryId>(groups.Count);
        var seen = new HashSet<EntryId>();

        foreach (var group in groups)
        {
            if (!groupStandings.TryGetValue(group.Id, out var standing))
            {
                throw new ApplicationFailureException(
                    $"No standing was provided for qualification group '{group.Id}'.",
                    ApplicationErrorCodes.QualificationStandingMissing);
            }

            if (standing.EntryAt(position) is not { } candidate)
            {
                continue;
            }

            if (!seen.Add(candidate))
            {
                throw new ApplicationFailureException(
                    $"Across-groups candidate '{candidate}' appears more than once.",
                    ApplicationErrorCodes.QualificationCandidateDuplicate);
            }

            candidates.Add(candidate);
        }

        return candidates.Count == 0
            ? throw new ApplicationFailureException(
                "Across-groups qualification has no candidates.",
                ApplicationErrorCodes.QualificationCandidatesEmpty)
            : CalculateStanding.Execute(candidates, matches, standingRules, penalties: penalties);
    }
}
