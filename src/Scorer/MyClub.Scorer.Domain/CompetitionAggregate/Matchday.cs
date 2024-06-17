// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.Collections;
using PropertyChanged;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public class Matchday : NameEntity, IMatchesProvider
    {
        private DateTime? _postponedDate;
        private readonly ExtendedObservableCollection<Match> _matches = [];
        private readonly IMatchdaysProvider _parent;

        public Matchday(IMatchdaysProvider parent, DateTime date, string name, string? shortName = null, Guid? id = null) : base(name, shortName, id)
        {
            _parent = parent;
            OriginDate = date;
            Matches = new(_matches);
        }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime OriginDate { get; set; }

        public DateTime Date => _postponedDate ?? OriginDate;

        public bool IsPostponed { get; private set; }

        public ReadOnlyObservableCollection<Match> Matches { get; }

        public void Postpone(DateTime? date = null, bool propagateToMatches = true)
        {
            IsPostponed = true;
            _postponedDate = date;
            RaisePropertyChanged(nameof(Date));

            if (propagateToMatches)
                Matches.Where(x => x.State is MatchState.None or MatchState.Postponed).ToList().ForEach(x => x.Postpone(date));
        }

        public void Schedule(DateTime? date = null, bool propagateToMatches = true)
        {
            IsPostponed = false;
            _postponedDate = date;
            RaisePropertyChanged(nameof(Date));

            if (propagateToMatches)
                Matches.Where(x => x.State is MatchState.None or MatchState.Postponed).ToList().ForEach(x =>
                {
                    x.Reset();
                    x.PostponedDate = _postponedDate;
                });
        }

        public Match AddMatch(ITeam homeTeam, ITeam awayTeam) => AddMatch(new Match(Date, homeTeam, awayTeam, _parent.ProvideFormat()));

        public Match AddMatch(DateTime date, ITeam homeTeam, ITeam awayTeam) => AddMatch(new Match(date, homeTeam, awayTeam, _parent.ProvideFormat()));

        public Match AddMatch(Match match)
        {
            _matches.Add(match);

            return match;
        }

        public bool RemoveMatch(Match item) => _matches.Remove(item);

        MatchFormat IMatchFormatProvider.ProvideFormat() => _parent.ProvideFormat();

        public override int CompareTo(object? obj) => obj is Matchday other ? OriginDate.CompareTo(other.OriginDate) : 1;

        public override string ToString() => Name;
    }
}
