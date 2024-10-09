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
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities;
using MyNet.Utilities.Collections;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class League : Championship, IMatchdaysStage, ICompetition
    {
        private readonly ExtendedObservableCollection<Matchday> _matchdays = [];

        public League() : this(RankingRules.Default, MatchFormat.Default, MatchRules.Default, SchedulingParameters.Default) { }

        public League(RankingRules rankingRules, MatchFormat matchFormat, MatchRules matchRules, SchedulingParameters schedulingParameters)
        {
            RankingRules = rankingRules;
            MatchFormat = matchFormat;
            MatchRules = matchRules;
            SchedulingParameters = schedulingParameters;
            Matchdays = new(_matchdays);
        }

        public RankingRules RankingRules { get; set; }

        public MatchFormat MatchFormat { get; set; }

        public MatchRules MatchRules { get; set; }

        public SchedulingParameters SchedulingParameters { get; set; }

        public ReadOnlyObservableCollection<Matchday> Matchdays { get; }

        public override RankingRules GetRankingRules() => RankingRules;

        MatchFormat IMatchFormatProvider.ProvideFormat() => MatchFormat;

        MatchRules IMatchRulesProvider.ProvideRules() => MatchRules;

        SchedulingParameters ISchedulingParametersProvider.ProvideSchedulingParameters() => SchedulingParameters;

        public override IEnumerable<Match> GetAllMatches() => Matchdays.SelectMany(x => x.GetAllMatches());

        public IEnumerable<T> GetStages<T>() where T : ICompetitionStage => Matchdays.OfType<T>();

        public Ranking GetRanking(Matchday matchday)
        {
            var matches = Matchdays.Where(x => x.OriginDate.IsBefore(matchday.OriginDate)).Union([matchday]).SelectMany(x => x.Matches);
            return new Ranking(Teams, matches, GetRankingRules(), GetPenaltyPoints(), Labels, (x, y) => x.State == MatchState.Played);
        }

        public override bool RemoveTeam(IVirtualTeam team)
        {
            _matchdays.ForEach(x => x.Matches.Where(x => x.Participate(team)).ToList().ForEach(y => x.RemoveMatch(y)));
            return base.RemoveTeam(team);
        }

        public bool RemoveMatch(Match item) => _matchdays.Any(x => x.RemoveMatch(item));

        #region Matchdays

        public Matchday AddMatchday(DateTime date, string name, string? shortName = null) => AddMatchday(new Matchday(this, date, name, shortName));

        public Matchday AddMatchday(Matchday matchday)
        {
            if (!ReferenceEquals(matchday.Stage, this))
                throw new ArgumentException("Matchday stage is not this league", nameof(matchday));

            if (Matchdays.Contains(matchday))
                throw new AlreadyExistsException(nameof(Matchdays), matchday);

            _matchdays.Add(matchday);

            return matchday;
        }

        public bool RemoveMatchday(Matchday item) => _matchdays.Remove(item);

        public void Clear() => _matchdays.Clear();

        #endregion
    }
}
