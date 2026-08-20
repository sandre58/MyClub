// -----------------------------------------------------------------------
// <copyright file="CompletionReasonDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Explains why a competition is not sportively complete (Application/Read — not Domain).
/// </summary>
/// <remarks>
/// Organizer copy lives in the SPA i18n layer keyed by <see cref="Code"/>.
/// </remarks>
/// <param name="Code">Stable machine code (e.g. ScheduledMatches).</param>
public sealed record CompletionReasonDto(string Code);
