// -----------------------------------------------------------------------
// <copyright file="ExtraTimeRule.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace MyClub.PlayUp.Domain.Rules;

/// <summary>
/// Marker value object: extra time may be used to resolve a tie when present on <see cref="TieFormat"/>.
/// Absence (<see langword="null"/>) means disabled. Distinct from <see cref="ExtraTimePolicy"/> (match clock).
/// </summary>
public sealed record ExtraTimeRule;
