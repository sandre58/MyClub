// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using System.Linq;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class CycleRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Cycle>(projectRepository, auditService), ICycleRepository
    {
        public override IEnumerable<Cycle> GetAll() => CurrentProject.Cycles;

        protected override Cycle AddCore(Cycle item) => CurrentProject.AddCycle(item);

        protected override IEnumerable<Cycle> AddRangeCore(IEnumerable<Cycle> items) => items.Select(AddCore);

        protected override bool RemoveCore(Cycle item) => CurrentProject.RemoveCycle(item);

        protected override int RemoveRangeCore(IEnumerable<Cycle> items) => items.Count(RemoveCore);
    }
}
