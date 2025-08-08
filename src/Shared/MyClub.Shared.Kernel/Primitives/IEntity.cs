// -----------------------------------------------------------------------
// <copyright file="IEntity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyNet.Utilities;

namespace MyClub.Shared.Kernel.Primitives;

/// <summary>
/// Marker interface for domain entities with strongly-typed identifiers.
/// Combines the IIdentifiable interface with constraints specific to DDD entities.
/// This interface ensures that all entities have a consistent identity contract.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
public interface IEntity<out TId> : IIdentifiable<TId>
    where TId : EntityId<TId>;
