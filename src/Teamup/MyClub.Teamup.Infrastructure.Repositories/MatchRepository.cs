// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class MatchRepository(IProjectRepository projectRepository, IAuditService auditService) : EntitiesRepositoryBase<Match>(projectRepository, auditService), IMatchRepository
    {
        public override IEnumerable<Match> GetAll() => CurrentProject.Competitions.SelectMany(x => x.GetAllMatches());

        public Match Insert(IHasMatches parent, DateTime date, Team homeTeam, Team awayTeam)
        {
            var added = parent.AddMatch(date, homeTeam, awayTeam);

            AuditCreatedItem(added);

            return added;
        }

        protected override Match AddCore(Match item) => item;

        protected override IEnumerable<Match> AddRangeCore(IEnumerable<Match> items) => items.Select(AddCore);

        protected override bool RemoveCore(Match item) => CurrentProject.Competitions.Any(x => x.RemoveMatch(item));

        protected override int RemoveRangeCore(IEnumerable<Match> items) => items.Count(RemoveCore);
    }
}
