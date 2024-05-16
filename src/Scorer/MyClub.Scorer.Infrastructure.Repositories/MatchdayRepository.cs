// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class MatchdayRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Matchday>(projectRepository, auditService), IMatchdayRepository
    {
        public override IEnumerable<Matchday> GetAll() => CurrentProject.Competition.GetAllMatchdaysProviders().SelectMany(x => x.Matchdays);

        public Matchday Insert(IMatchdaysProvider parent, DateTime date, string name, string? shortName = null)
        {
            var added = parent.AddMatchday(date, name, shortName);

            AuditCreatedItem(added);

            return added;
        }
        protected override Matchday AddCore(Matchday item) => item;

        protected override bool DeleteCore(Matchday item) => CurrentProject.Competition.GetAllMatchdaysProviders().Any(x => x.RemoveMatchday(item));
    }
}
