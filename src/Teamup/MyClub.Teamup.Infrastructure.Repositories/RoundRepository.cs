// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class RoundRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<IRound>(projectRepository, auditService), IRoundRepository
    {
        public override IEnumerable<IRound> GetAll() => CurrentProject.Competitions.OfType<CupSeason>().SelectMany(x => x.Rounds);

        public IRound Insert(CupSeason parent, IRound item)
        {
            var added = parent.AddRound(item);

            AuditCreatedItem(added);

            return added;
        }

        protected override IRound AddCore(IRound item) => item;

        protected override bool DeleteCore(IRound item) => CurrentProject.Competitions.OfType<CupSeason>().Any(x => x.RemoveRound(item.Id));
    }
}
