// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public interface IChampionship : IEntity
    {
        ChampionshipRules Rules { get; }

        ReadOnlyObservableCollection<Team> Teams { get; }

        IDictionary<Team, int>? Penalties { get; }

        IEnumerable<Match> GetAllMatches();

        Ranking GetRanking();

        Ranking GetHomeRanking();

        Ranking GetAwayRanking();
    }
}
