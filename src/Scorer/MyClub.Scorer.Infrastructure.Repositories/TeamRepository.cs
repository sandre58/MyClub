// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class TeamRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Team>(projectRepository, auditService), ITeamRepository
    {
        public override IEnumerable<Team> GetAll() => CurrentProject.Teams;

        protected override Team AddCore(Team item) => CurrentProject.AddTeam(item);

        protected override IEnumerable<Team> AddRangeCore(IEnumerable<Team> items) => items.Select(AddCore);

        protected override bool RemoveCore(Team item) => CurrentProject.RemoveTeam(item);

        protected override int RemoveRangeCore(IEnumerable<Team> items) => items.Count(RemoveCore);
    }
}
