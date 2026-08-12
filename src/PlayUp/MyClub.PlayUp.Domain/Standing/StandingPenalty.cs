// -----------------------------------------------------------------------
// <copyright file="StandingPenalty.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Standing;

/// <summary>
/// Data-only standing penalty snapshot for calculation (no aggregate references).
/// Deduction is applied only by <see cref="StandingCalculator"/>.
/// </summary>
public sealed record StandingPenalty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StandingPenalty"/> class.
    /// </summary>
    /// <param name="entryId">Targeted entry.</param>
    /// <param name="pointsDeducted">Points to deduct (&gt; 0).</param>
    public StandingPenalty(EntryId entryId, int pointsDeducted)
    {
        if (pointsDeducted <= 0)
        {
            throw new DomainException(
                "Standing penalty points deducted must be greater than zero.",
                StandingErrorCodes.PenaltyInvalid);
        }

        EntryId = entryId;
        PointsDeducted = pointsDeducted;
    }

    /// <summary>
    /// Gets the targeted entry identity.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the points to deduct.
    /// </summary>
    public int PointsDeducted { get; }
}
