// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyClub.Domain;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.TeamAggregate;


namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public interface ICompetitionSeason : IAuditableEntity, IHasPeriod
    {
        ICompetition Competition { get; }

        Season Season { get; }

        ReadOnlyObservableCollection<Team> Teams { get; }

        CompetitionRules Rules { get; set; }

        IEnumerable<Match> GetAllMatches();

        bool RemoveMatch(Match match);

        void SetTeams(IEnumerable<Team> teams);

        Team AddTeam(Team team);

        bool RemoveTeam(Team team);
    }
}

