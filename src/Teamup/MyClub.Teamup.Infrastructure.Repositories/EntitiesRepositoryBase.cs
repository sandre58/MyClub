// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Domain;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public abstract class EntitiesRepositoryBase<T> : IRepository<T>
        where T : IAuditableEntity
    {
        private readonly IAuditService _auditService;
        private readonly IProjectRepository _projectRepository;

        protected Project CurrentProject => _projectRepository.GetCurrentOrThrow();

        protected bool HasCurrentProject => _projectRepository.HasCurrent();

        protected EntitiesRepositoryBase(IProjectRepository projectRepository, IAuditService auditService)
            => (_projectRepository, _auditService) = (projectRepository, auditService);

        public abstract IEnumerable<T> GetAll();

        public virtual T? GetById(Guid id) => GetAll().GetByIdOrDefault(id);

        protected T GetByIdOrThrow(Guid id) => GetById(id) ?? throw new ArgumentException($"{nameof(T)} '{id}' not found", nameof(id));

        public T Insert(T item)
        {
            var added = AddCore(item);

            AuditCreatedItem(added);

            return added;
        }

        protected abstract T AddCore(T item);

        public bool Remove(Guid id) => DeleteCore(GetByIdOrThrow(id));

        protected abstract bool DeleteCore(T item);

        public T Update(T item)
        {
            var newItem = UpdateCore(GetByIdOrThrow(item.Id), item);

            AuditUpdatedItem(newItem);

            return newItem;
        }

        protected virtual T UpdateCore(T item, T newItem) => item;

        public void Save() => throw new InvalidOperationException("Save method is not used in this use case.");

        protected void AuditCreatedItem(T item) => _auditService.New(item);

        protected void AuditUpdatedItem(T item) => _auditService.Update(item);
    }
}
