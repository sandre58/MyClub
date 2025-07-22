// -----------------------------------------------------------------------
// <copyright file="IAuditableEntity.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using MyClub.Shared.Kernel.Interfaces;

namespace MyClub.Shared.Kernel.Primitives;

public interface IAuditableEntity<TId> : IEntity<TId>, IAuditable
    where TId : EntityId<TId>
{ }
