// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities.Deferring;
using MyClub.Application.Dtos;
using MyClub.Application.Helpers;
using MyClub.Domain;

namespace MyClub.Application.Services
{
    public abstract class CrudService<T, TDto, TRepository> : ICrudService<T, TDto>
        where T : IEntity
        where TDto : EntityDto
        where TRepository : IRepository<T>
    {
        protected TRepository Repository { get; }

        protected Deferrer CollectionChangedDeferrer { get; }

        protected CrudService(TRepository repository)
        {
            Repository = repository;
            CollectionChangedDeferrer = new Deferrer(OnCollectionChanged);
        }

        public IList<T> GetAll() => Repository.GetAll().ToList();

        public T? GetById(Guid id) => Repository.GetById(id);

        public virtual T Save(TDto dto)
        {
            var isNew = IsNew(dto.Id);
            var newItem = isNew ? Add(dto) : Update(dto);

            return newItem;
        }

        public IList<T> Save(IEnumerable<TDto> dtos, bool replaceOldItems = false)
        {
            using (CollectionChangedDeferrer.Defer())
            {
                var oldList = GetAll();
                var newIds = dtos.Select(x => x.Id).ToList();

                if (replaceOldItems)
                    oldList.Where(x => !newIds.Contains(x.Id)).ToList().ForEach(x => Remove(x.Id));

                return dtos.Select(Save).ToList();
            }
        }

        private T Add(TDto dto)
        {
            var item = CreateEntity(dto);
            var added = Repository.Insert(item);

            LogHelper.LogChangeAction(LogHelper.ChangeAction.Add, added);

            OnItemAdded(added);
            CollectionChangedDeferrer.DeferOrExecute();

            return added;
        }

        private T Update(TDto dto) => Update(dto.Id!.Value, x => UpdateEntity(x, dto));

        protected T Update(Guid id, Action<T> update)
        {
            var entity = Repository.GetById(id) ?? throw new InvalidOperationException("item is null");

            update(entity);
            var updated = Repository.Update(entity);

            LogHelper.LogChangeAction(LogHelper.ChangeAction.Update, updated);
            CollectionChangedDeferrer.DeferOrExecute();

            return updated;
        }

        protected abstract T CreateEntity(TDto dto);

        protected abstract void UpdateEntity(T entity, TDto dto);

        public virtual bool Remove(Guid id)
        {
            var item = Repository.GetById(id);

            if (item is null) return false;

            var removed = Repository.Remove(id);

            if (removed)
            {
                LogHelper.LogChangeAction(LogHelper.ChangeAction.Removed, item);

                OnItemRemoved(item);
                CollectionChangedDeferrer.DeferOrExecute();
            }

            return removed;
        }

        public int Remove(IEnumerable<Guid> ids)
        {
            using (CollectionChangedDeferrer.Defer())
                return ids.Count(Remove);
        }

        protected bool IsNew(Guid? id) => id is null || Repository.GetById(id.Value) is null;

        protected virtual void OnCollectionChanged() { }

        protected virtual void OnItemAdded(T item) { }

        protected virtual void OnItemRemoved(T item) { }
    }
}
