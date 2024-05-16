// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Exceptions;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.DateTimes;


namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public abstract class CompetitionSeason<TRules> : AuditableEntity, IAggregateRoot, ICompetitionSeason
        where TRules : CompetitionRules
    {
        private readonly ObservableCollection<Team> _teams = [];

        protected CompetitionSeason(ICompetition competition, Season season, TRules rules, DateTime? startDate = null, DateTime? endDate = null, Guid? id = null)
            : base(id)
        {
            Competition = competition;
            Season = season;
            Rules = rules;
            Teams = new(_teams);
            Period = new(startDate ?? season.Period.Start, endDate ?? season.Period.End);
        }

        public ICompetition Competition { get; }

        public Season Season { get; }

        public ObservablePeriod Period { get; }

        public ReadOnlyObservableCollection<Team> Teams { get; }

        public TRules Rules { get; set; }

        CompetitionRules ICompetitionSeason.Rules { get => Rules; set => Rules = (TRules)value; }

        public abstract IEnumerable<Match> GetAllMatches();

        public abstract bool RemoveMatch(Match match);

        private int RemoveMatchesOfTeam(Team team)
        {
            var matches = GetAllMatches().Where(x => x.Participate(team)).ToList();

            return matches.Count(RemoveMatch);
        }

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

        #endregion

        public override string ToString() => $"{Competition} {Season}";
    }
}

