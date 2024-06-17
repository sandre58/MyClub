// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace MyClub.Domain
{
    public interface IRepository<T> where T : IEntity
    {
        T Insert(T item);

        IEnumerable<T> InsertRange(IEnumerable<T> items);

        T Update(T item);

        IEnumerable<T> UpdateRange(IEnumerable<T> items);

        bool Remove(Guid id);

        int RemoveRange(IEnumerable<Guid> ids);

        T? GetById(Guid id);

        IEnumerable<T> GetAll();

        void Save();
    }
}
