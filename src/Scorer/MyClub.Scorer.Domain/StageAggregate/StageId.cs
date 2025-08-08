// -----------------------------------------------------------------------
// <copyright file="StageId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.StageAggregate;

/// <summary>
/// Represents a strongly-typed identifier for Stage entities.
/// This ensures type safety and prevents mixing of different entity identifiers.
/// </summary>
/// <param name="Value">The underlying GUID value for the stage identifier.</param>
public sealed record StageId(Guid Value) : EntityId<StageId>(Value);
