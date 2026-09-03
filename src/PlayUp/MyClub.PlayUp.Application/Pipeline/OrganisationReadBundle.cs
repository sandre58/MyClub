// -----------------------------------------------------------------------
// <copyright file="OrganisationReadBundle.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Lightweight competition read snapshot for the Organisation hub.
/// </summary>
/// <param name="Competition">Loaded competition.</param>
/// <param name="Stages">Structure + slots for readiness checks.</param>
/// <param name="SheetMemberRefs">Declared members still on a match sheet.</param>
internal sealed record OrganisationReadBundle(
    Competition Competition,
    IReadOnlyList<Stage> Stages,
    IReadOnlyList<MatchSheetMemberRef> SheetMemberRefs);
