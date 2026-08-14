// -----------------------------------------------------------------------
// <copyright file="EntrySideDto.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Application.Reads;

/// <summary>
/// Participant side for a match read (home or away).
/// </summary>
/// <param name="EntryId">Competition entry identity.</param>
/// <param name="DisplayName">Display name when known from the competition; otherwise <see langword="null"/>.</param>
public sealed record EntrySideDto(Guid EntryId, string? DisplayName);
