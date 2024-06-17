// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TacticAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class TacticRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Tactic>(projectRepository, auditService), ITacticRepository
    {
        public override IEnumerable<Tactic> GetAll() => CurrentProject.Tactics;

        protected override Tactic AddCore(Tactic item) => CurrentProject.AddTactic(item);

        protected override IEnumerable<Tactic> AddRangeCore(IEnumerable<Tactic> items) => items.Select(AddCore);

        protected override bool RemoveCore(Tactic item) => CurrentProject.RemoveTactic(item);

        protected override int RemoveRangeCore(IEnumerable<Tactic> items) => items.Count(RemoveCore);
    }
}
