// -----------------------------------------------------------------------
// <copyright file="AwayGoalsRule.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Marker value object: away-goals rule is enabled when present on <see cref="TieFormat"/>.
/// Absence (<see langword="null"/>) means disabled.
/// </summary>
public sealed record AwayGoalsRule;
