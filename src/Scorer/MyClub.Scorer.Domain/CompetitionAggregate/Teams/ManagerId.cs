// -----------------------------------------------------------------------
// <copyright file="ManagerId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.CompetitionAggregate.Teams;

/// <summary>
/// Represents a strongly-typed identifier for Manager entities.
/// This ensures type safety and prevents mixing of different entity identifiers.
/// </summary>
/// <param name="Value">The underlying GUID value for the manager identifier.</param>
public sealed record ManagerId(Guid Value) : EntityId<ManagerId>(Value);
