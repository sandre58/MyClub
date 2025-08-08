// -----------------------------------------------------------------------
// <copyright file="PlayerId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

/// <summary>
/// Represents a strongly-typed identifier for Player entities.
/// This ensures type safety and prevents mixing of different entity identifiers.
/// </summary>
/// <param name="Value">The underlying GUID value for the player identifier.</param>
public sealed record PlayerId(Guid Value) : EntityId<PlayerId>(Value);
