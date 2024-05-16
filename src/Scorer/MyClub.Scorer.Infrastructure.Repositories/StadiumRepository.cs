// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class StadiumRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Stadium>(projectRepository, auditService), IStadiumRepository
    {
        public override IEnumerable<Stadium> GetAll() => CurrentProject.Stadiums;

        protected override Stadium AddCore(Stadium item) => CurrentProject.AddStadium(item);

        protected override bool DeleteCore(Stadium item) => CurrentProject.RemoveStadium(item);
    }
}
