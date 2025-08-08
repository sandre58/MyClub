// -----------------------------------------------------------------------
// <copyright file="PenaltyShootoutId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.MatchAggregate.MatchEvents;

/// <summary>
/// Represents a strongly-typed identifier for PenaltyShootout entities.
/// This ensures type safety and prevents mixing of different entity identifiers.
/// </summary>
/// <param name="Value">The underlying GUID value for the penalty shootout identifier.</param>
public sealed record PenaltyShootoutId(Guid Value) : EntityId<PenaltyShootoutId>(Value);
