// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class TeamRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Team>(projectRepository, auditService), ITeamRepository
    {
        public override IEnumerable<Team> GetAll() => CurrentProject.Competitions.SelectMany(x => x.Teams).Distinct();

        protected override Team AddCore(Team item) => throw new InvalidOperationException("Add method is not used in this context");

        protected override IEnumerable<Team> AddRangeCore(IEnumerable<Team> items) => items.Select(AddCore);

        protected override bool RemoveCore(Team item) => throw new InvalidOperationException("Delete method is not used in this context");

        protected override int RemoveRangeCore(IEnumerable<Team> items) => items.Count(RemoveCore);
    }
}
