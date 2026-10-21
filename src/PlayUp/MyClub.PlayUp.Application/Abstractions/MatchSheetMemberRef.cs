// -----------------------------------------------------------------------
// <copyright file="MatchSheetMemberRef.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Application.Abstractions;

/// <summary>
/// Tabular projection: a declared member referenced on a match composition sheet.
/// </summary>
/// <param name="EntryId">Owning entry (resolved from match side).</param>
/// <param name="MemberId">Declared member identity.</param>
public sealed record MatchSheetMemberRef(EntryId EntryId, MemberId MemberId);
