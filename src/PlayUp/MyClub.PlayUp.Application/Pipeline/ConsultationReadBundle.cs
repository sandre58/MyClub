// -----------------------------------------------------------------------
// <copyright file="ConsultationReadBundle.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Common;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Competition read snapshot for Consultation (Results / Standings / Structure).
/// </summary>
/// <param name="Competition">Loaded competition.</param>
/// <param name="Stages">Full stage graphs in canonical order.</param>
/// <param name="MatchesByStage">Summary rows keyed by stage.</param>
internal sealed record ConsultationReadBundle(
    Competition Competition,
    IReadOnlyList<Stage> Stages,
    IReadOnlyDictionary<StageId, IReadOnlyList<MatchSummaryRow>> MatchesByStage);
