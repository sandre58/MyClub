// -----------------------------------------------------------------------
// <copyright file="GroupSeatModes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Stages;

using ApplicationErrorCodes = MyClub.PlayUp.Application.ApplicationErrorCodes;

namespace MyClub.PlayUp.Application.Stages;

/// <summary>
/// Seat modes projected on a Groups schematic case.
/// </summary>
public static class GroupSeatModes
{
    /// <summary>An empty placeable seat, or an occupied manual seat.</summary>
    public const string Editable = "Editable";

    /// <summary>The occupant arrived by qualification or progression.</summary>
    public const string QualProg = "QualProg";

    /// <summary>The occupant is sealed by an applied group draw.</summary>
    public const string Draw = "Draw";

    /// <summary>The target group already has an attached match.</summary>
    public const string MatchLocked = "MatchLocked";

    /// <summary>The empty seat is held for an unresolved qualification or progression feed.</summary>
    public const string Reserved = "Reserved";

    /// <summary>A non-cancelled, non-applied group draw is still on the phase.</summary>
    public const string DrawPending = "DrawPending";

    /// <summary>
    /// Maps the first refusal code of the matching route to a seat mode.
    /// A lifecycle lock and every other code stay <see cref="Editable"/>; they are not a seventh mode.
    /// </summary>
    /// <param name="refusalCode">The first refusal, or <see langword="null"/> when the route would succeed.</param>
    /// <returns>The seat mode.</returns>
    public static string FromRefusal(string? refusalCode) =>
        refusalCode switch
        {
            ApplicationErrorCodes.ManualGroupPlacementBlockedByActiveDraw => DrawPending,
            ApplicationErrorCodes.AppliedDrawGroupPlacementProtected => Draw,
            StageErrorCodes.GroupPlacementResolutionLocked => QualProg,
            StageErrorCodes.GroupAssignLockedByGroupMatches => MatchLocked,
            ApplicationErrorCodes.GroupSeatReservedByInboundFeed => Reserved,
            _ => Editable,
        };
}
