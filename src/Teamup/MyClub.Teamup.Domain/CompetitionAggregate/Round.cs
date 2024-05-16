// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.Utilities;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public abstract class Round<TRules> : NameEntity, IRound
        where TRules : CompetitionRules
    {
        private readonly ObservableCollection<Team> _teams = [];

        protected Round(string name, string shortName, TRules rules, Guid? id = null) : base(name, shortName, id)
        {
            Teams = new(_teams);
            Rules = rules;
        }

        public ReadOnlyObservableCollection<Team> Teams { get; }

        public TRules Rules { get; set; }

        CompetitionRules IRound.Rules
        {
            get => Rules;
            set => Rules = (TRules)value;
        }

        public abstract bool RemoveMatch(Match match);

        public abstract IEnumerable<Match> GetAllMatches();

        #region Teams

        public void SetTeams(IEnumerable<Team> teams) => _teams.UpdateFrom(teams, x => AddTeam(x), x => RemoveTeam(x), (_, _) => { }, (x, y) => x.Id == y.Id);

        public Team AddTeam(Team team)
        {
            if (Teams.Contains(team))
                throw new AlreadyExistsException(nameof(Teams), team);

            _teams.Add(team);

            return team;
        }

        public bool RemoveTeam(Team team)
        {
            _ = RemoveMatchesOfTeam(team);
            return _teams.Remove(team);
        }

        private int RemoveMatchesOfTeam(Team team)
        {
            var matches = GetAllMatches().Where(x => x.Participate(team)).ToList();

            return matches.Count(RemoveMatch);
        }

        #endregion
    }
}
