// -----------------------------------------------------------------------
// <copyright file="ReplaceStageDefaultTieFormatRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing the stage default TieFormat.
/// <see cref="Clear"/> true clears the default. Legs must be 1 or 2.
/// Two legs imply aggregate scoring; away goals require two legs.
/// </summary>
public sealed record ReplaceStageDefaultTieFormatRequest(
    bool Clear = false,
    int NumberOfLegs = 1,
    bool HasAwayGoalsRule = false,
    bool HasExtraTimeRule = false,
    bool HasPenaltyShootoutRule = false);
