// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.StadiumAggregate;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class StadiumRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Stadium>(projectRepository, auditService), IStadiumRepository
    {
        public override IEnumerable<Stadium> GetAll() => CurrentProject.Stadiums;

        protected override Stadium AddCore(Stadium item) => CurrentProject.AddStadium(item);

        protected override IEnumerable<Stadium> AddRangeCore(IEnumerable<Stadium> items) => items.Select(AddCore);

        protected override bool RemoveCore(Stadium item) => CurrentProject.RemoveStadium(item);

        protected override int RemoveRangeCore(IEnumerable<Stadium> items) => items.Count(RemoveCore);
    }
}
