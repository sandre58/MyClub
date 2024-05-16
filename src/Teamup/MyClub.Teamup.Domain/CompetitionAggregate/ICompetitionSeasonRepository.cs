// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using MyClub.Domain;
using MyClub.Teamup.Domain.MatchAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public interface ICompetitionSeasonRepository : IRepository<ICompetitionSeason>
    {
        IHasMatches GetMatchesParent(Guid id);

        IHasMatchdays GetMatchdaysParent(Guid id);
    }
}
