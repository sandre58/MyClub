// -----------------------------------------------------------------------
// <copyright file="PenaltyShootoutRule.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Marker value object: a penalty shootout may be used to resolve a tie when present on <see cref="TieFormat"/>.
/// Absence (<see langword="null"/>) means disabled. Distinct from <see cref="PenaltyShootoutPolicy"/> (match TAB parameters).
/// </summary>
public sealed record PenaltyShootoutRule;
