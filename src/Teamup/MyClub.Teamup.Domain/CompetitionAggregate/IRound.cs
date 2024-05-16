// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MyNet.Utilities;
using MyClub.Domain;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public interface IRound : IAuditableEntity, ISimilar
    {
        string Name { get; set; }

        string ShortName { get; set; }

        ReadOnlyObservableCollection<Team> Teams { get; }

        CompetitionRules Rules { get; set; }

        void SetTeams(IEnumerable<Team> teams);

        Team AddTeam(Team team);

        bool RemoveTeam(Team team);

        bool RemoveMatch(Match match);

        IEnumerable<Match> GetAllMatches();
    }
}
