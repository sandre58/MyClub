// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;

namespace MyClub.Domain
{
    public abstract class AuditableEntity : Entity, IAuditableEntity
    {
        protected AuditableEntity(Guid? id = null) : base(id) { }

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
}
