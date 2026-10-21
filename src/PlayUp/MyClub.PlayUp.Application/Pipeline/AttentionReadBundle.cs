// -----------------------------------------------------------------------
// <copyright file="AttentionReadBundle.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Lightweight competition read snapshot for Needs Attention and workspace summary.
/// </summary>
/// <param name="Competition">Loaded competition.</param>
/// <param name="Stages">Stages with attention-required collections.</param>
/// <param name="MatchesByStage">Attention slices keyed by stage.</param>
internal sealed record AttentionReadBundle(
    Competition Competition,
    IReadOnlyList<Stage> Stages,
    IReadOnlyDictionary<StageId, IReadOnlyList<MatchAttentionSlice>> MatchesByStage);
