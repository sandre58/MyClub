// -----------------------------------------------------------------------
// <copyright file="MatchId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Domain.Matches;

/// <summary>
/// Strongly-typed identifier for match entities.
/// Provides type safety and prevents accidental mixing of match identifiers with other entity identifiers.
/// This follows the strongly-typed ID pattern used throughout the domain to ensure compile-time safety.
/// </summary>
/// <param name="Value">The underlying GUID value for this match identifier.</param>
public sealed record MatchId(Guid Value) : EntityId<MatchId>(Value);
