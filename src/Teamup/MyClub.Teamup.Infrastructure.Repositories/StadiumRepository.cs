// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.StadiumAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class StadiumRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Stadium>(projectRepository, auditService), IStadiumRepository
    {
        public override IEnumerable<Stadium> GetAll() => HasCurrentProject ? CurrentProject.Competitions.SelectMany(x => x.Teams).Select(x => x.Stadium.Value)
                                                        .Concat(CurrentProject.Competitions.SelectMany(x => x.GetAllMatches()).Select(x => x.Stadium))
                                                        .Concat([CurrentProject.Club.Stadium])
                                                        .NotNull()
                                                        .Distinct() : [];

        protected override Stadium AddCore(Stadium item) => throw new InvalidOperationException("Add method is not used in this context");

        protected override IEnumerable<Stadium> AddRangeCore(IEnumerable<Stadium> items) => items.Select(AddCore);

        protected override bool RemoveCore(Stadium item) => throw new InvalidOperationException("Delete method is not used in this context");

        protected override int RemoveRangeCore(IEnumerable<Stadium> items) => items.Count(RemoveCore);
    }
}
