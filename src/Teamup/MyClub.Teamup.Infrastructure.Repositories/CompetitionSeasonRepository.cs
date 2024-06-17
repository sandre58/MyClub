// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Domain.Services;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.ProjectAggregate;

namespace MyClub.Teamup.Infrastructure.Repositories
{
    public class CompetitionSeasonRepository(IProjectRepository projectRepository, IAuditService auditService)
        : EntitiesRepositoryBase<ICompetitionSeason>(projectRepository, auditService), ICompetitionSeasonRepository
    {
        public override IEnumerable<ICompetitionSeason> GetAll() => CurrentProject.Competitions;

        public IHasMatchdays GetMatchdaysParent(Guid id)
            => CurrentProject.Competitions.OfType<IHasMatchdays>().GetByIdOrDefault(id) // League
                ?? CurrentProject.Competitions.OfType<CupSeason>().SelectMany(x => x.Rounds).OfType<IHasMatchdays>().GetById(id); // GroupStage of Cup

        public IHasMatches GetMatchesParent(Guid id)
            => CurrentProject.Competitions.OfType<IHasMatches>().GetByIdOrDefault(id) // Friendly
                ?? CurrentProject.Competitions.OfType<IHasMatchdays>().SelectMany(x => x.Matchdays).GetByIdOrDefault(id) // Matchday of League
                ?? CurrentProject.Competitions.OfType<CupSeason>().SelectMany(x => x.Rounds).OfType<IHasMatches>().GetByIdOrDefault(id) // Knockout of cup
                ?? CurrentProject.Competitions.OfType<CupSeason>().SelectMany(x => x.Rounds).OfType<IHasMatchdays>().SelectMany(x => x.Matchdays).GetById(id); // Matchday of GroupStage of Cup

        protected override ICompetitionSeason AddCore(ICompetitionSeason item)
        {
            if (!item.Competition.Seasons.Contains(item))
                item.Competition.AddSeason(item);

            return CurrentProject.AddCompetition(item);
        }

        protected override IEnumerable<ICompetitionSeason> AddRangeCore(IEnumerable<ICompetitionSeason> items) => items.Select(AddCore);

        protected override bool RemoveCore(ICompetitionSeason item)
        {
            item.Competition.RemoveSeason(item);

            return CurrentProject.RemoveCompetition(item);
        }

        protected override int RemoveRangeCore(IEnumerable<ICompetitionSeason> items) => items.Count(RemoveCore);
    }
}
