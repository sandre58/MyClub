// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class Group : NameEntity, IChampionship, IOrderable
    {
        private readonly ObservableCollection<Team> _teams = [];

        public Group(GroupStage groupStage, string name, string shortName, Guid? id = null)
            : base(name, shortName, id)
        {
            GroupStage = groupStage;
            Teams = new(_teams);
        }

        public GroupStage GroupStage { get; }

        public IDictionary<Team, int>? Penalties { get; set; }

        public ChampionshipRules Rules => GroupStage.Rules;

        public ReadOnlyObservableCollection<Team> Teams { get; }

        public int Order { get; set; }

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

            return matches.Count(GroupStage.RemoveMatch);
        }

        #endregion

        public Ranking GetRanking() => new(this);

        public Ranking GetHomeRanking() => new(Teams, GetAllMatches(), Rules.RankingRules, null, (x, y) => y == x.HomeTeam && x.State == MatchState.Played);

        public Ranking GetAwayRanking() => new(Teams, GetAllMatches(), Rules.RankingRules, null, (x, y) => y == x.AwayTeam && x.State == MatchState.Played);

        public IEnumerable<Match> GetAllMatches() => GroupStage.Matchdays.SelectMany(x => x.Matches).Where(x => Teams.Any(y => x.Participate(y)));
    }
}

