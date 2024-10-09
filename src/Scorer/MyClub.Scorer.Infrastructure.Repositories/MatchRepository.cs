// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.ProjectAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.DateTimes;

namespace MyClub.Scorer.Infrastructure.Repositories
{
    public class MatchRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Match>(projectRepository, auditService), IMatchRepository
    {
        public override IEnumerable<Match> GetAll() => CurrentProject.Competition.GetAllMatches();

        public IEnumerable<Match> GetByPeriod(Period period) => GetAll().Where(x => period.IntersectWith(x.GetPeriod()));

        public IEnumerable<Match> GetMatchesInStadium(Guid stadiumId, Period? period = null)
            => (period is null ? GetAll() : GetByPeriod(period)).Where(x => x.Stadium is not null && x.Stadium.Id == stadiumId);

        public IEnumerable<Match> GetMatchesOfTeams(IEnumerable<Guid> teamIds, Period? period = null)
            => (period is null ? GetAll() : GetByPeriod(period)).Where(x => teamIds.Any(y => x.Participate(y)));

        public Match Insert(Guid stageId, DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam)
        {
            var stage = CurrentProject.Competition.GetStage<IMatchesStage>(stageId) ?? throw new ArgumentException("Stage not found", nameof(stageId));
            var added = stage.AddMatch(date, homeTeam, awayTeam) ?? throw new InvalidOperationException("Match could not be added");

            AuditNewItem(added);

            return added;
        }

        protected override Match AddCore(Match item) => item;

        protected override IEnumerable<Match> AddRangeCore(IEnumerable<Match> items) => items.Select(AddCore);

        protected override bool RemoveCore(Match item) => CurrentProject.Competition.RemoveMatch(item);

        protected override int RemoveRangeCore(IEnumerable<Match> items) => items.Count(RemoveCore);
    }
}
