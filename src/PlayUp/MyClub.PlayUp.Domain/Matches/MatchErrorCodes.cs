// -----------------------------------------------------------------------
// <copyright file="MatchErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Matches;

/// <summary>
/// Stable machine-readable codes for Match aggregate invariant violations.
/// </summary>
public static class MatchErrorCodes
{
    /// <summary>
    /// Gets the code for an invalid lifecycle transition.
    /// </summary>
    public const string InvalidTransition = "Match.InvalidTransition";

    /// <summary>
    /// Gets the code when home and away entry identities are the same.
    /// </summary>
    public const string SameParticipant = "Match.SameParticipant";

    /// <summary>
    /// Gets the code when a match result or score is invalid.
    /// </summary>
    public const string InvalidResult = "Match.InvalidResult";

    /// <summary>
    /// Gets the code when a result is already recorded (Finished).
    /// </summary>
    public const string ResultAlreadyRecorded = "Match.ResultAlreadyRecorded";
}
