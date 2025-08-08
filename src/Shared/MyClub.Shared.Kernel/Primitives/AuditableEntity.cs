// -----------------------------------------------------------------------
// <copyright file="AuditableEntity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using JetBrains.Annotations;

namespace MyClub.Shared.Kernel.Primitives;

/// <summary>
/// Base class for domain entities that require audit tracking.
/// Extends Entity to include creation and modification timestamps and user information.
/// This class is ideal for entities where you need to track who created or modified them and when.
/// </summary>
/// <typeparam name="TId">The strongly-typed identifier for the entity, which must inherit from EntityId&lt;TId&gt;.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity<TId>
    where TId : EntityId<TId>
{
    [UsedImplicitly(Reason = "Used by EF Core.")]
    protected AuditableEntity() { }

    protected AuditableEntity(TId id)
        : base(id) { }

    /// <summary>
    /// Gets the timestamp when this entity was created.
    /// </summary>
    public DateTime? CreatedAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who created this entity.
    /// </summary>
    public string? CreatedBy { get; private set; }

    /// <summary>
    /// Gets the timestamp when this entity was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; private set; }

    /// <summary>
    /// Gets the identifier of the user who last modified this entity.
    /// </summary>
    public string? ModifiedBy { get; private set; }

    /// <summary>
    /// Marks the entity as modified with the specified timestamp and user.
    /// This method is typically called by the infrastructure layer when saving changes.
    /// </summary>
    /// <param name="modifiedAt">The timestamp when the entity was modified.</param>
    /// <param name="modifiedBy">The identifier of the user who modified the entity. Can be null if not available.</param>
    public virtual void MarkedAsModified(DateTime? modifiedAt, string? modifiedBy = null)
    {
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Marks the entity as created with the specified timestamp and user.
    /// This method clears any previous modification information and sets the creation audit data.
    /// This method is typically called by the infrastructure layer when creating a new entity.
    /// </summary>
    /// <param name="createdAt">The timestamp when the entity was created.</param>
    /// <param name="createdBy">The identifier of the user who created the entity. Can be null if not available.</param>
    public virtual void MarkedAsCreated(DateTime? createdAt, string? createdBy = null)
    {
        ModifiedAt = null;
        ModifiedBy = null;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }
}
