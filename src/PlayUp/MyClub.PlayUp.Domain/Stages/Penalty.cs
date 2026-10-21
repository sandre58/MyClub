// -----------------------------------------------------------------------
// <copyright file="Penalty.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Standing points deduction owned by a Stage and targeting a competition entry.
/// Applicable solely by presence in <see cref="Stage.Penalties"/> (no Active/Revoked status).
/// Distinct from <c>PenaltyShootoutScore</c> (match TAB) and from FairPlay.
/// </summary>
[DebuggerDisplay("{EntryId} -{PointsDeducted}")]
public sealed class Penalty : Entity<PenaltyId>
{
    internal Penalty(PenaltyId id, EntryId entryId, int pointsDeducted, string? reason)
        : base(id)
    {
        if (pointsDeducted <= 0)
        {
            throw new DomainException(
                "Penalty points deducted must be greater than zero.",
                StageErrorCodes.PenaltyInvalid);
        }

        EntryId = entryId;
        PointsDeducted = pointsDeducted;
        Reason = NormalizeReason(reason);
    }

    /// <summary>
    /// Gets the targeted competition entry.
    /// </summary>
    public EntryId EntryId { get; }

    /// <summary>
    /// Gets the points to subtract from standing totals (&gt; 0).
    /// </summary>
    public int PointsDeducted { get; }

    /// <summary>
    /// Gets an optional free-text reason (traceability only; not used by standing calculation).
    /// </summary>
    public string? Reason { get; }

    private static string? NormalizeReason(string? reason)
    {
        if (reason is null)
        {
            return null;
        }

        var trimmed = reason.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
