// -----------------------------------------------------------------------
// <copyright file="ResultReason.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Common;

/// <summary>
/// Reason accompanying a non-played match result (<see cref="ResultType"/> other than <see cref="ResultType.Played"/>).
/// </summary>
public enum ResultReason
{
    /// <summary>
    /// Opponent declared a forfeit.
    /// </summary>
    OpponentForfeit = 0,

    /// <summary>
    /// A team withdrew from the competition.
    /// </summary>
    TeamWithdrawn = 1,

    /// <summary>
    /// Organizer took an administrative decision.
    /// </summary>
    AdministrativeDecision = 2
}
