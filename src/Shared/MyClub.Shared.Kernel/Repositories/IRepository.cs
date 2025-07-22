// -----------------------------------------------------------------------
// <copyright file="IRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Kernel.Repositories;

public interface IRepository<TEntity, TId> : IReadOnlyRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
{
    void Add(TEntity entity);

    void AddRange(IEnumerable<TEntity> entities);

    void Update(TEntity entity);

    void Update(IEnumerable<TEntity> entities);

    int Delete(TId id);

    int DeleteRange(IEnumerable<TId> ids);
}
