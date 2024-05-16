// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Domain.Enums;
using PropertyChanged;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class Knockout : Round<CupRules>, IHasMatches
    {
        private DateTime? _postponedDate;
        private readonly ObservableCollection<Match> _matches = [];

        public Knockout(string name, string shortName, DateTime date, CupRules? rules = null, Guid? id = null) : base(name, shortName, rules ?? CupRules.Default, id)
        {
            OriginDate = date;
            Matches = new(_matches);
        }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime OriginDate { get; set; }

        public DateTime Date => _postponedDate ?? OriginDate;

        public bool IsPostponed { get; private set; }

        MatchFormat IHasMatches.MatchFormat => Rules.MatchFormat;

        public ReadOnlyObservableCollection<Match> Matches { get; }

        public void Postpone(DateTime? date = null)
        {
            IsPostponed = true;
            _postponedDate = date;
            RaisePropertyChanged(nameof(Date));

            Matches.Where(x => x.State is MatchState.None or MatchState.Postponed).ToList().ForEach(x => x.Postpone(date));
        }

        public void Schedule(DateTime? date = null)
        {
            IsPostponed = false;
            _postponedDate = date;
            RaisePropertyChanged(nameof(Date));

            Matches.Where(x => x.State is MatchState.None or MatchState.Postponed).ToList().ForEach(x =>
            {
                x.Reset();
                x.PostponedDate = Date;
            });
        }

        public Match AddMatch(Team homeTeam, Team awayTeam) => AddMatch(new Match(Date, homeTeam, awayTeam, Rules.MatchFormat));

        public Match AddMatch(DateTime date, Team homeTeam, Team awayTeam) => AddMatch(new Match(date, homeTeam, awayTeam, Rules.MatchFormat));

        public Match AddMatch(Match match)
        {
            _matches.Add(match);

            return match;
        }

        public override bool RemoveMatch(Match match) => _matches.Remove(match);

        public override IEnumerable<Match> GetAllMatches() => _matches;
    }
}
