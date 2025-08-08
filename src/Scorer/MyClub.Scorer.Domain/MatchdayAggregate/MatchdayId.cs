// -----------------------------------------------------------------------
// <copyright file="MatchdayId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchdayAggregate;

/// <summary>
/// Represents a strongly-typed identifier for Matchday entities.
/// This ensures type safety and prevents mixing of different entity identifiers.
/// </summary>
/// <param name="Value">The underlying GUID value for the matchday identifier.</param>
public sealed record MatchdayId(Guid Value) : EntityId<MatchdayId>(Value);
