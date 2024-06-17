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

        public IEnumerable<Match> GetMatchesInStadium(Guid stadiumId, Period? period = null)
            => (period is null ? GetAll() : GetByPeriod(period)).Where(x => x.Stadium is not null && x.Stadium.Id == stadiumId);

        public IEnumerable<Match> GetMatchesOfTeams(IEnumerable<Guid> teamIds, Period? period = null)
            => (period is null ? GetAll() : GetByPeriod(period)).Where(x => teamIds.Any(y => x.Participate(y)));

        public Match Insert(IMatchesProvider parent, DateTime date, ITeam homeTeam, ITeam awayTeam)
        {
            var added = parent.AddMatch(date, homeTeam, awayTeam);

            AuditCreatedItem(added);

            return added;
        }
        protected override Match AddCore(Match item) => item;

        protected override IEnumerable<Match> AddRangeCore(IEnumerable<Match> items) => items.Select(AddCore);

        protected override bool RemoveCore(Match item) => CurrentProject.Competition.GetAllMatchesProviders().Any(x => x.RemoveMatch(item));

        protected override int RemoveRangeCore(IEnumerable<Match> items) => items.Count(RemoveCore);
    }
}
