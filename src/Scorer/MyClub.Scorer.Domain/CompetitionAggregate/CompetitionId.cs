// -----------------------------------------------------------------------
// <copyright file="CompetitionId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.CompetitionAggregate;

/// <summary>
/// Represents a strongly-typed identifier for Competition entities.
/// This ensures type safety and prevents mixing of different entity identifiers.
/// </summary>
/// <param name="Value">The underlying GUID value for the competition identifier.</param>
public sealed record CompetitionId(Guid Value) : EntityId<CompetitionId>(Value);
