// -----------------------------------------------------------------------
// <copyright file="DeclaredParticipation.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// A player declared on a match composition sheet for one side.
/// Identity is <see cref="MemberId"/> within the owning <see cref="Match"/> — not a global person identity.
/// </summary>
[DebuggerDisplay("{Side} {CompositionStatus} ({Id})")]
public sealed class DeclaredParticipation : Entity<MemberId>
{
    internal DeclaredParticipation(MemberId id, Side side, CompositionStatus compositionStatus, int? jerseyNumber)
        : base(id)
    {
        EnsureDefinedSide(side);
        EnsureDefinedCompositionStatus(compositionStatus);
        Side = side;
        CompositionStatus = compositionStatus;
        JerseyNumber = jerseyNumber;
    }

    /// <summary>
    /// Gets the match side for this participation.
    /// </summary>
    public Side Side { get; }

    /// <summary>
    /// Gets the declared composition status (starter or bench).
    /// </summary>
    public CompositionStatus CompositionStatus { get; private set; }

    /// <summary>
    /// Gets the optional jersey number for this match (not a value object in V1).
    /// </summary>
    public int? JerseyNumber { get; private set; }

    internal void ChangeCompositionStatus(CompositionStatus compositionStatus)
    {
        EnsureDefinedCompositionStatus(compositionStatus);
        CompositionStatus = compositionStatus;
    }

    internal void SetJerseyNumber(int? jerseyNumber) => JerseyNumber = jerseyNumber;

    private static void EnsureDefinedSide(Side side)
    {
        if (!Enum.IsDefined(side))
        {
            throw new DomainException(
                $"Unknown match side '{side}'.",
                MatchErrorCodes.InvalidSide);
        }
    }

    private static void EnsureDefinedCompositionStatus(CompositionStatus compositionStatus)
    {
        if (!Enum.IsDefined(compositionStatus))
        {
            throw new DomainException(
                $"Unknown composition status '{compositionStatus}'.",
                MatchErrorCodes.InvalidCompositionStatus);
        }
    }
}
