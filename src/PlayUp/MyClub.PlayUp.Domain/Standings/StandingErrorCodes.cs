// -----------------------------------------------------------------------
// <copyright file="StandingErrorCodes.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Standings;

/// <summary>
/// Stable machine-readable codes for Standing calculation invariant violations.
/// </summary>
public static class StandingErrorCodes
{
    /// <summary>
    /// Gets the code when participants are empty or contain duplicates.
    /// </summary>
    public const string ParticipantsInvalid = "Standing.ParticipantsInvalid";

    /// <summary>
    /// Gets the code when a match filter value is unknown.
    /// </summary>
    public const string MatchFilterInvalid = "Standing.MatchFilterInvalid";

    /// <summary>
    /// Gets the code when standing rules are missing.
    /// </summary>
    public const string RulesRequired = "Standing.RulesRequired";

    /// <summary>
    /// Gets the code when a standing penalty snapshot is invalid.
    /// </summary>
    public const string PenaltyInvalid = "Standing.PenaltyInvalid";
}
