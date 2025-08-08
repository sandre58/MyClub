// -----------------------------------------------------------------------
// <copyright file="StadiumId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Domain.Stadiums;

/// <summary>
/// Strongly-typed identifier for stadium entities.
/// Provides type safety and prevents accidental mixing of stadium identifiers with other entity identifiers.
/// This follows the strongly-typed ID pattern used throughout the domain to ensure compile-time safety.
/// </summary>
/// <param name="Value">The underlying GUID value for this stadium identifier.</param>
public sealed record StadiumId(Guid Value) : EntityId<StadiumId>(Value);
