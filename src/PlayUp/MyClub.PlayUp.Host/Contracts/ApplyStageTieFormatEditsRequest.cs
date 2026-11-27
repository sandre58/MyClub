// -----------------------------------------------------------------------
// <copyright file="ApplyStageTieFormatEditsRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for atomic default + round TieFormat edits on a stage.
/// </summary>
public sealed record ApplyStageTieFormatEditsRequest(
    bool ReplaceDefault = false,
    ReplaceStageDefaultTieFormatRequest? Default = null,
    IReadOnlyList<ApplyStageTieFormatRoundEditRequest>? RoundEdits = null);
