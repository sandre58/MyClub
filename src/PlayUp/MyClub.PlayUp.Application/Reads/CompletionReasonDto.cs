// -----------------------------------------------------------------------
// <copyright file="CompletionReasonDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Explains why a competition is not sportively complete (Application/Read — not Domain).
/// </summary>
/// <param name="Code">Stable machine code (e.g. ScheduledMatches).</param>
/// <param name="Message">Organizer-facing explanation.</param>
public sealed record CompletionReasonDto(string Code, string Message);
