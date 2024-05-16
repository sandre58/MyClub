// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TacticAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class TacticRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Tactic>(projectRepository, auditService), ITacticRepository
    {
        public override IEnumerable<Tactic> GetAll() => CurrentProject.Tactics;

        protected override Tactic AddCore(Tactic item) => CurrentProject.AddTactic(item);

        protected override bool DeleteCore(Tactic item) => CurrentProject.RemoveTactic(item);
    }
}
