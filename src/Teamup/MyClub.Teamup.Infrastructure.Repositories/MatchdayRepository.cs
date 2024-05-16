// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class MatchdayRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Matchday>(projectRepository, auditService), IMatchdayRepository
    {
        public override IEnumerable<Matchday> GetAll()
            => CurrentProject.Competitions.OfType<IHasMatchdays>().SelectMany(x => x.Matchdays).Concat(CurrentProject.Competitions.OfType<CupSeason>().SelectMany(x => x.Rounds.OfType<IHasMatchdays>().SelectMany(y => y.Matchdays)));

        public Matchday Insert(IHasMatchdays parent, Matchday item)
        {
            var added = parent.AddMatchday(item);

            AuditCreatedItem(added);

            return added;
        }

        public Matchday Insert(IHasMatchdays parent, string name, DateTime date, string? shortName = null)
        {
            var added = parent.AddMatchday(name, date, shortName);

            AuditCreatedItem(added);

            return added;
        }

        protected override Matchday AddCore(Matchday item) => item;

        protected override bool DeleteCore(Matchday item)
            => CurrentProject.Competitions.OfType<IHasMatchdays>().Any(x => x.RemoveMatchday(item.Id)) || CurrentProject.Competitions.OfType<CupSeason>().SelectMany(x => x.Rounds.OfType<IHasMatchdays>()).Any(x => x.RemoveMatchday(item.Id));
    }
}
