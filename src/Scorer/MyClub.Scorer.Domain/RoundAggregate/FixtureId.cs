// -----------------------------------------------------------------------
// <copyright file="FixtureId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Scorer.Domain.RoundAggregate;

/// <summary>
/// Represents a strongly-typed identifier for Fixture entities.
/// This ensures type safety and prevents mixing of different entity identifiers.
/// </summary>
/// <param name="Value">The underlying GUID value for the fixture identifier.</param>
public sealed record FixtureId(Guid Value) : EntityId<FixtureId>(Value);
