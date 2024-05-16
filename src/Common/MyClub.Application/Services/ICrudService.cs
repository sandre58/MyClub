// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Application.Dtos;
using MyClub.Domain;

namespace MyClub.Application.Services
{
    public interface ICrudService<T, TDto> : IListService<T>, ICrudService
        where T : IEntity
        where TDto : EntityDto
    {
        public T Save(TDto dto);

        IList<T> Save(IEnumerable<TDto> dtos, bool replaceOldItems = false);
    }

    public interface ICrudService
    {
        bool Remove(Guid id);

        int Remove(IEnumerable<Guid> ids);
    }

    public interface IListService<T> where T : IEntity
    {
        public IList<T> GetAll();

        public T? GetById(Guid id);
    }
}
