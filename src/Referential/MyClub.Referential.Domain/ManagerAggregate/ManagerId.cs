// -----------------------------------------------------------------------
// <copyright file="ManagerId.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Referential.Domain.ManagerAggregate;

/// <summary>
/// Represents a strongly-typed unique identifier for Manager entities, providing type safety
/// and preventing identifier confusion in the football domain. This value object ensures
/// compile-time validation of manager references throughout the MyClub ecosystem.
/// </summary>
/// <param name="Value">The underlying GUID value that uniquely identifies a manager.</param>
public sealed record ManagerId(Guid Value) : EntityId<ManagerId>(Value);
