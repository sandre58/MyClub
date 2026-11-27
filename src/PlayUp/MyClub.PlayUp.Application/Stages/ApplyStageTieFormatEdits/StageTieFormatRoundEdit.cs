// -----------------------------------------------------------------------
// <copyright file="StageTieFormatRoundEdit.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Rules;

namespace MyClub.PlayUp.Application.Stages.ApplyStageTieFormatEdits;

/// <summary>
/// One round TieFormat replace or clear (inherit default).
/// </summary>
public sealed record StageTieFormatRoundEdit(RoundId RoundId, TieFormat? TieFormat);