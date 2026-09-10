// -----------------------------------------------------------------------
// <copyright file="ReplaceStageDrawRulesRequest.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Host.Contracts;

/// <summary>
/// HTTP body for replacing DrawRules on a stage.
/// <see cref="Clear"/> true (or omit Mode) clears DrawRules.
/// V1 mode is typically <see cref="DrawMode.Random"/>.
/// </summary>
public sealed record ReplaceStageDrawRulesRequest(
    bool Clear = false,
    DrawMode? Mode = null,
    int? NumberOfPots = null,
    int? NumberOfSeeds = null);
