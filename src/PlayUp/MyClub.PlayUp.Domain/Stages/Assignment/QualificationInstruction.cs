// -----------------------------------------------------------------------
// <copyright file="QualificationInstruction.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Domain.Stages;

/// <summary>
/// Result of applying a qualification path: add the resolved entry to phase population.
/// Not persisted — Application mutates <see cref="Stage"/> via population resolution apply.
/// </summary>
/// <param name="StageId">Destination stage.</param>
/// <param name="EntryId">Resolved competition entry.</param>
public sealed record QualificationInstruction(StageId StageId, EntryId EntryId);
