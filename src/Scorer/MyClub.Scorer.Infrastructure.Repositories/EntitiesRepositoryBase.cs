// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyNet.Utilities;
using MyClub.Domain;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Domain.Services;
using System.Linq;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public abstract class EntitiesRepositoryBase<T> : IRepository<T>
        where T : IAuditableEntity
    {
        private readonly IAuditService _auditService;
        private readonly IProjectRepository _projectRepository;

        protected IProject CurrentProject => _projectRepository.GetCurrentOrThrow();

        protected bool HasCurrentProject => _projectRepository.HasCurrent();

        protected EntitiesRepositoryBase(IProjectRepository projectRepository, IAuditService auditService)
            => (_projectRepository, _auditService) = (projectRepository, auditService);

        public abstract IEnumerable<T> GetAll();

        public virtual T? GetById(Guid id) => GetAll().GetByIdOrDefault(id);

        protected T GetByIdOrThrow(Guid id) => GetById(id) ?? throw new ArgumentException($"{nameof(T)} '{id}' not found", nameof(id));

        public T Insert(T item)
        {
            var added = AddCore(item);

            AuditNewItem(added);

            return added;
        }

        public IEnumerable<T> InsertRange(IEnumerable<T> items)
        {
            var added = AddRangeCore(items).ToList();

            added.ForEach(x => AuditNewItem(x));

            return added;
        }

        protected abstract T AddCore(T item);

        protected abstract IEnumerable<T> AddRangeCore(IEnumerable<T> items);

        public bool Remove(Guid id) => RemoveCore(GetByIdOrThrow(id));

        public int RemoveRange(IEnumerable<Guid> ids) => RemoveRangeCore(ids.Select(GetByIdOrThrow).ToList());

        protected abstract bool RemoveCore(T item);

        protected abstract int RemoveRangeCore(IEnumerable<T> items);

        public T Update(T item)
        {
            var newItem = UpdateCore(GetByIdOrThrow(item.Id), item);

            AuditExistingItem(newItem);

            return newItem;
        }

        public IEnumerable<T> UpdateRange(IEnumerable<T> items)
        {
            var newItems = items.Select(x => UpdateCore(GetByIdOrThrow(x.Id), x)).ToList();

            newItems.ForEach(x => AuditExistingItem(x));

            return newItems;
        }

        protected virtual T UpdateCore(T item, T newItem) => item;

        public void Save() => throw new InvalidOperationException("Save method is not used in this use case.");

        protected void AuditNewItem(IAuditable item) => _auditService.New(item);

        protected void AuditExistingItem(IAuditable item) => _auditService.Update(item);
    }
}
