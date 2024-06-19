// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class League : Championship, ICompetition, IMatchdaysProvider
    {
        private readonly ExtendedObservableCollection<Matchday> _matchdays = [];

        public League() : this(RankingRules.Default, MatchFormat.Default) { }

        public League(RankingRules rankingRules, MatchFormat matchFormat)
        {
            RankingRules = rankingRules;
            MatchFormat = matchFormat;
            Matchdays = new(_matchdays);
        }

        public RankingRules RankingRules { get; set; }

        public MatchFormat MatchFormat { get; set; }

        public ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        public override RankingRules GetRankingRules() => RankingRules;

        public override IEnumerable<Match> GetAllMatches() => Matchdays.SelectMany(x => x.Matches);

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        public IEnumerable<IMatchdaysProvider> GetAllMatchdaysProviders() => new[] { this };

        public IEnumerable<IMatchesProvider> GetAllMatchesProviders() => Matchdays;

        public Ranking GetRanking(Matchday matchday)
        {
            var matches = Matchdays.Where(x => x.OriginDate.IsBefore(matchday.OriginDate)).Union([matchday]).SelectMany(x => x.Matches);
            return new Ranking(Teams, matches, GetRankingRules(), GetPenaltyPoints(), Labels, (x, y) => x.State == MatchState.Played);
        }

        public override bool RemoveTeam(Team team)
        {
            _matchdays.ForEach(x => x.Matches.Where(x => x.Participate(team)).ToList().ForEach(y => x.RemoveMatch(y)));
            return base.RemoveTeam(team);
        }

        #region Matchdays

        public Matchday AddMatchday(DateTime date, string name, string? shortName = null) => AddMatchday(new Matchday(this, date, name, shortName));

        public Matchday AddMatchday(Matchday matchday)
        {
            if (Matchdays.Contains(matchday))
                throw new AlreadyExistsException(nameof(Matchdays), matchday);

            _matchdays.Add(matchday);

            return matchday;
        }

        public bool RemoveMatchday(Matchday item) => _matchdays.Remove(item);

        #endregion
    }
}
