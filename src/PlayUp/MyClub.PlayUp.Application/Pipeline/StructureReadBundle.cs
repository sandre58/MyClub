// -----------------------------------------------------------------------
// <copyright file="StructureReadBundle.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Application.Abstractions;
using MyClub.PlayUp.Domain.Competitions;
using MyClub.PlayUp.Domain.Stages;

namespace MyClub.PlayUp.Application.Pipeline;

/// <summary>
/// Lightweight competition read snapshot for the Structure hub.
/// </summary>
/// <param name="Competition">Loaded competition.</param>
/// <param name="Stages">Structure + slots for readiness checks.</param>
/// <param name="SheetMemberRefs">Declared members still on a match sheet.</param>
internal sealed record StructureReadBundle(
    Competition Competition,
    IReadOnlyList<Stage> Stages,
    IReadOnlyList<MatchSheetMemberRef> SheetMemberRefs);
