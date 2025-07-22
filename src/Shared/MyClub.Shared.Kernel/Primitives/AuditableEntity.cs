// -----------------------------------------------------------------------
// <copyright file="AuditableEntity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace MyClub.Shared.Kernel.Primitives;

public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity<TId>
    where TId : EntityId<TId>
{
    // <remarks>Used by EF Core</remarks>
    protected AuditableEntity()
        : base() { }

    protected AuditableEntity(TId id)
        : base(id) { }

    public DateTime? CreatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    public string? ModifiedBy { get; private set; }

    public virtual void MarkedAsModified(DateTime? modifiedAt, string? modifiedBy = null)
    {
        ModifiedAt = modifiedAt;
        ModifiedBy = modifiedBy;
    }

    public virtual void MarkedAsCreated(DateTime? createdAt, string? createdBy = null)
    {
        ModifiedAt = null;
        ModifiedBy = null;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }
}
