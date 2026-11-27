// -----------------------------------------------------------------------
// <copyright file="ApplyStageTieFormatRoundEditRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// One per-round TieFormat edit inside <see cref="ApplyStageTieFormatEditsRequest"/>.
/// </summary>
public sealed record ApplyStageTieFormatRoundEditRequest(
    Guid RoundId,
    bool Clear = false,
    int NumberOfLegs = 1,
    bool HasAwayGoalsRule = false,
    bool HasExtraTimeRule = false,
    bool HasPenaltyShootoutRule = false);
