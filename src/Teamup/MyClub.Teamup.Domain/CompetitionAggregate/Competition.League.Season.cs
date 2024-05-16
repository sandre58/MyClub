// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyNet.Utilities;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Domain.Enums;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class LeagueSeason : CompetitionSeason<LeagueRules>, IChampionship, IHasMatchdays
    {
        private readonly ObservableCollection<Matchday> _matchdays = [];

        public LeagueSeason(League competition, Season season, LeagueRules rules, DateTime? startDate = null, DateTime? endDate = null, Guid? id = null)
            : base(competition, season, rules, startDate, endDate, id) => Matchdays = new(_matchdays);

        public IDictionary<Team, int>? Penalties { get; set; }

        public ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        ChampionshipRules IChampionship.Rules => Rules;

        ChampionshipRules IHasMatchdays.Rules => Rules;

        public Ranking GetRanking() => new(this);

        public Ranking GetHomeRanking() => new(Teams, GetAllMatches(), Rules.RankingRules, null, (x, y) => y == x.HomeTeam && x.State == MatchState.Played);

        public Ranking GetAwayRanking() => new(Teams, GetAllMatches(), Rules.RankingRules, null, (x, y) => y == x.AwayTeam && x.State == MatchState.Played);

        public override IEnumerable<Match> GetAllMatches() => Matchdays.SelectMany(x => x.Matches);

        public Matchday AddMatchday(string name, DateTime date, string? shortName = null)
        {
            var datetime = date.ToLocalTime().TimeOfDay == TimeSpan.Zero ? date.AddFluentTimeSpan(Rules.MatchTime) : date;
            return AddMatchday(new Matchday(name, datetime, shortName, Rules.MatchFormat));
        }

        public Matchday AddMatchday(Matchday matchday)
        {
            _matchdays.Add(matchday);

            return matchday;
        }

        public bool RemoveMatchday(Matchday matchday) => _matchdays.Remove(matchday);

        public bool RemoveMatchday(Guid matchdayId) => _matchdays.HasId(matchdayId) && _matchdays.Remove(_matchdays.GetById(matchdayId));

        public void ClearMatchdays() => _matchdays.Clear();

        public override bool RemoveMatch(Match match) => Matchdays.Any(x => x.RemoveMatch(match));
    }
}

