// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MyClub.Domain
{
    public interface IRepository<T> where T : IEntity
    {
        T Insert(T item);

        T Update(T item);

        bool Remove(Guid id);

        T? GetById(Guid id);

        IEnumerable<T> GetAll();

        void Save();
    }
}
