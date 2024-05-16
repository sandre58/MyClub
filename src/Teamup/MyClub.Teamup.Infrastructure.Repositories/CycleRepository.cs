// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Teamup.Domain.CycleAggregate;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class CycleRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Cycle>(projectRepository, auditService), ICycleRepository
    {
        public override IEnumerable<Cycle> GetAll() => CurrentProject.Cycles;

        protected override Cycle AddCore(Cycle item) => CurrentProject.AddCycle(item);

        protected override bool DeleteCore(Cycle item) => CurrentProject.RemoveCycle(item);
    }
}
