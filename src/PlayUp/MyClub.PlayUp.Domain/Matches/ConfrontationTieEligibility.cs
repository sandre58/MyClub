// -----------------------------------------------------------------------
// <copyright file="ConfrontationTieEligibility.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Domain-only confrontation tie aggregate eligibility (no I/O).
/// </summary>
public static class ConfrontationTieEligibility
{
    /// <summary>
    /// Issues a proof when play scores leave the confrontation level after aggregate (and away goals when active).
    /// </summary>
    /// <param name="tieFormat">Effective tie format for the confrontation.</param>
    /// <param name="entryA">First distinct entry across legs.</param>
    /// <param name="entryB">Second distinct entry across legs.</param>
    /// <param name="legs">Ordered confrontation legs including proposed return scores.</param>
    /// <returns>An eligibility proof.</returns>
    /// <exception cref="DomainException">Thrown when the aggregate is not level after play scores.</exception>
    public static ConfrontationTieEligibilityProof CreateProof(
        TieFormat tieFormat,
        EntryId entryA,
        EntryId entryB,
        IReadOnlyList<FixtureLegSnapshot> legs)
    {
        ArgumentNullException.ThrowIfNull(tieFormat);
        ArgumentNullException.ThrowIfNull(legs);

        if (!TwoLegAggregateStillLevel.AfterPlayScores(tieFormat, entryA, entryB, legs))
        {
            throw new DomainException(
                "Confrontation tie penalty shootout requires a level aggregate after play scores.",
                MatchErrorCodes.InvalidResult);
        }

        return new ConfrontationTieEligibilityProof();
    }
}
