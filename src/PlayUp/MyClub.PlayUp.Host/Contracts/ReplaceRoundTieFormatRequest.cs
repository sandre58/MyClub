// -----------------------------------------------------------------------
// <copyright file="ReplaceRoundTieFormatRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing a Round TieFormat.
/// <see cref="Clear"/> true clears the round format (effective OneLeg). Legs must be 1 or 2 (V1).
/// Two legs imply aggregate scoring; away goals require two legs.
/// </summary>
public sealed record ReplaceRoundTieFormatRequest(
    bool Clear = false,
    int NumberOfLegs = 1,
    bool HasAwayGoalsRule = false,
    bool HasExtraTimeRule = false,
    bool HasPenaltyShootoutRule = false);
