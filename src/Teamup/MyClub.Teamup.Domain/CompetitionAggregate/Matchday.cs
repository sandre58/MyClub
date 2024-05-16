// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Teamup.Domain.MatchAggregate;
using MyClub.Teamup.Domain.TeamAggregate;
using MyClub.Domain;
using MyClub.Domain.Enums;
using PropertyChanged;

namespace MyClub.Teamup.Domain.CompetitionAggregate
{
    public class Matchday : NameEntity, IHasMatches
    {
        private DateTime? _postponedDate;
        private readonly ObservableCollection<Match> _matches = [];

        public Matchday(string name, DateTime date, string? shortName = null, MatchFormat? matchFormat = null, Guid? id = null) : base(name, shortName, id)
        {
            OriginDate = date;
            Matches = new(_matches);
            MatchFormat = matchFormat ?? MatchFormat.Default;
        }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime OriginDate { get; set; }

        public DateTime Date => _postponedDate ?? OriginDate;

        public bool IsPostponed { get; private set; }

        public ReadOnlyObservableCollection<Match> Matches { get; }

        public MatchFormat MatchFormat { get; }

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

        public Match AddMatch(Team homeTeam, Team awayTeam) => AddMatch(new Match(Date, homeTeam, awayTeam, MatchFormat));

        public Match AddMatch(DateTime date, Team homeTeam, Team awayTeam) => AddMatch(new Match(date, homeTeam, awayTeam, MatchFormat));

        public Match AddMatch(Match match)
        {
            _matches.Add(match);

            return match;
        }

        public bool RemoveMatch(Match match) => _matches.Remove(match);

        public override string ToString() => Name;
    }
}
