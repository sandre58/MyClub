// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.PersonAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class ManagerRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Manager>(projectRepository, auditService), IManagerRepository
    {
        public override IEnumerable<Manager> GetAll() => CurrentProject.Teams.SelectMany(x => x.Staff);

        public Manager Insert(Team team, string firstName, string lastName)
        {
            var added = team.AddManager(firstName, lastName);

            AuditNewItem(added);

            return added;
        }

        protected override Manager AddCore(Manager item) => item;

        protected override IEnumerable<Manager> AddRangeCore(IEnumerable<Manager> items) => items.Select(AddCore);

        protected override bool RemoveCore(Manager item) => CurrentProject.Teams.Any(x => x.RemoveManager(item));

        protected override int RemoveRangeCore(IEnumerable<Manager> items) => items.Count(RemoveCore);
    }
}
