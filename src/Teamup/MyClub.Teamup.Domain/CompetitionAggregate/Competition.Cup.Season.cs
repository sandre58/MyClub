// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.SeasonAggregate;
using MyNet.Utilities;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class CupSeason : CompetitionSeason<CupRules>
    {
        private readonly ObservableCollection<IRound> _rounds = [];

        public CupSeason(Cup competition, Season season, CupRules rules, DateTime? startDate = null, DateTime? endDate = null, Guid? id = null)
            : base(competition, season, rules, startDate, endDate, id) => Rounds = new(_rounds);

        public ReadOnlyObservableCollection<IRound> Rounds { get; }

        public override IEnumerable<Match> GetAllMatches() => Rounds.SelectMany(x => x.GetAllMatches());

        public Knockout AddKnockout(string name, string shortName, DateTime date, CupRules? rules = null)
        {
            var datetime = date.ToLocalTime().TimeOfDay == TimeSpan.Zero ? date.AddFluentTimeSpan(Rules.MatchTime) : date;
            return (Knockout)AddRound(new Knockout(name, shortName, datetime, rules ?? Rules));
        }

        public GroupStage AddGroupStage(string name, string shortName, DateTime startDate, DateTime endDate, ChampionshipRules? rules = null)
            => (GroupStage)AddRound(new GroupStage(name, shortName, startDate, endDate, rules ?? new ChampionshipRules(Rules.MatchFormat, RankingRules.Default, Rules.MatchTime)));

        public IRound AddRound(IRound round)
        {
            _rounds.Add(round);

            return round;
        }

        public bool RemoveRound(IRound round) => _rounds.Remove(round);

        public bool RemoveRound(Guid roundId) => _rounds.HasId(roundId) && _rounds.Remove(_rounds.GetById(roundId));

        public void ClearRounds() => _rounds.Clear();

        public override bool RemoveMatch(Match match) => Rounds.Any(x => x.RemoveMatch(match));
    }
}

