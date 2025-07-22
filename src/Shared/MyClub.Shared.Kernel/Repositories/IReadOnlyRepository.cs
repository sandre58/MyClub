// -----------------------------------------------------------------------
// <copyright file="IReadOnlyRepository.cs" company="Stéphane ANDRE">
// Copyright (c) Stéphane ANDRE. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MyClub.Shared.Kernel.Primitives;

namespace MyClub.Shared.Kernel.Repositories;

public interface IReadOnlyRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : EntityId<TId>
{
    TEntity? GetById(TId id);

    IReadOnlyList<TEntity> GetAll(Expression<Func<TEntity, bool>>? predicate = null);

    bool Exists(TId id);

    int Count(Expression<Func<TEntity, bool>>? predicate = null);
}
