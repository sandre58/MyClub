// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class MatchRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Match>(projectRepository, auditService), IMatchRepository
    {
        public override IEnumerable<Match> GetAll() => CurrentProject.Competition.GetAllMatchesProviders().SelectMany(x => x.Matches);

        public IEnumerable<Match> GetByPeriod(Period period) => GetAll().Where(x => period.Intersect(x.GetPeriod()));

        public Match Insert(IMatchesProvider parent, DateTime date, ITeam homeTeam, ITeam awayTeam)
        {
            var added = parent.AddMatch(date, homeTeam, awayTeam);

            AuditCreatedItem(added);

            return added;
        }
        protected override Match AddCore(Match item) => item;

        protected override bool DeleteCore(Match item) => CurrentProject.Competition.GetAllMatchesProviders().Any(x => x.RemoveMatch(item));
    }
}
