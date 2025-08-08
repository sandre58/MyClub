// -----------------------------------------------------------------------
// <copyright file="PlayerId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.PlayerAggregate;

/// <summary>
/// Represents a strongly-typed unique identifier for Player entities, providing type safety
/// and preventing identifier confusion in the football domain. This value object ensures
/// compile-time validation of player references throughout the MyClub ecosystem.
/// </summary>
/// <param name="Value">The underlying GUID value that uniquely identifies a player.</param>
public sealed record PlayerId(Guid Value) : EntityId<PlayerId>(Value);
