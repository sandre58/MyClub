// -----------------------------------------------------------------------
// <copyright file="RecordedSubstitution.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Ordered substitution fact on a match (player out / player in) — distinct from declared composition.
/// Does not carry absolute time; relative order is preserved by the owning <see cref="Match"/>.
/// </summary>
[DebuggerDisplay("{Side}: {OutMemberId} → {InMemberId} ({Id})")]
public sealed class RecordedSubstitution : Entity<SubstitutionId>
{
    internal RecordedSubstitution(
        SubstitutionId id,
        Side side,
        MemberId outMemberId,
        MemberId inMemberId)
        : base(id)
    {
        EnsureDefinedSide(side);
        Side = side;
        OutMemberId = outMemberId;
        InMemberId = inMemberId;
    }

    /// <summary>
    /// Gets the match side for this substitution.
    /// </summary>
    public Side Side { get; private set; }

    /// <summary>
    /// Gets the member leaving the field.
    /// </summary>
    public MemberId OutMemberId { get; private set; }

    /// <summary>
    /// Gets the member entering the field.
    /// </summary>
    public MemberId InMemberId { get; private set; }

    internal void Correct(Side side, MemberId outMemberId, MemberId inMemberId)
    {
        EnsureDefinedSide(side);
        Side = side;
        OutMemberId = outMemberId;
        InMemberId = inMemberId;
    }

    private static void EnsureDefinedSide(Side side)
    {
        if (!Enum.IsDefined(side))
        {
            throw new DomainException(
                $"Unknown match side '{side}'.",
                MatchErrorCodes.InvalidSide);
        }
    }
}
