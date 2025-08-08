// -----------------------------------------------------------------------
// <copyright file="IAuditableEntity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Interfaces;

namespace MyClub.Shared.Kernel.Primitives;

/// <summary>
/// Interface for domain entities that support audit tracking.
/// Combines entity identity with audit capabilities for tracking creation and modification history.
/// Entities implementing this interface will have their audit information automatically managed by the infrastructure layer.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
public interface IAuditableEntity<out TId> : IEntity<TId>, IAuditable
    where TId : EntityId<TId>;
