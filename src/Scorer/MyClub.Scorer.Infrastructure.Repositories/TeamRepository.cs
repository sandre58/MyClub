// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.TeamAggregate;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class TeamRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Team>(projectRepository, auditService), ITeamRepository
    {
        public override IEnumerable<Team> GetAll() => CurrentProject.Teams;

        protected override Team AddCore(Team item) => CurrentProject.AddTeam(item);

        protected override bool DeleteCore(Team item) => CurrentProject.RemoveTeam(item);
    }
}
