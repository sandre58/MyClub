// -----------------------------------------------------------------------
// <copyright file="GeneratedSquadMember.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.PlayUp.Domain.Common;

namespace MyClub.PlayUp.Development.Generators;

/// <summary>
/// One seeded roster row (declared member + optional match jersey).
/// </summary>
/// <param name="DisplayName">Member display name.</param>
/// <param name="Role">Declared role.</param>
/// <param name="JerseyNumber">Optional jersey used on match sheets.</param>
public sealed record GeneratedSquadMember(
    string DisplayName,
    DeclaredMemberRole Role,
    int? JerseyNumber);
