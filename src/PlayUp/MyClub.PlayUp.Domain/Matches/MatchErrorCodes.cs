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

    /// <summary>
    /// Gets the code when the match composition cannot be mutated in the current status.
    /// </summary>
    public const string CompositionImmutable = "Match.CompositionImmutable";

    /// <summary>
    /// Gets the code when a MemberId is already present on the match composition.
    /// </summary>
    public const string DuplicateParticipation = "Match.DuplicateParticipation";

    /// <summary>
    /// Gets the code when a declared participation is not found on the match.
    /// </summary>
    public const string ParticipationNotFound = "Match.ParticipationNotFound";

    /// <summary>
    /// Gets the code when a jersey number is already used on the same side.
    /// </summary>
    public const string DuplicateJerseyNumber = "Match.DuplicateJerseyNumber";

    /// <summary>
    /// Gets the code when a composition status value is invalid.
    /// </summary>
    public const string InvalidCompositionStatus = "Match.InvalidCompositionStatus";

    /// <summary>
    /// Gets the code when a match side value is invalid.
    /// </summary>
    public const string InvalidSide = "Match.InvalidSide";
}
