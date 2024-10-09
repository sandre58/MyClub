// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyClub.Domain;
using MyClub.Domain.Enums;
using MyClub.Domain.Exceptions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Domain.TeamAggregate;
using MyNet.Utilities.Collections;
using PropertyChanged;

namespace MyClub.Scorer.Domain.CompetitionAggregate
{
    public abstract class MatchesStage<T> : NameEntity, IMatchesStage where T : Match
    {
        private DateTime? _postponedDate;
        private readonly ExtendedObservableCollection<T> _matches = [];

        public MatchesStage(DateTime date, string name, string? shortName = null, Guid? id = null) : base(name, shortName, id)
        {
            OriginDate = date;
            Matches = new(_matches);
        }

        [AlsoNotifyFor(nameof(Date))]
        public DateTime OriginDate { get; set; }

        public DateTime Date => _postponedDate ?? OriginDate;

        public bool IsPostponed { get; private set; }

        public ReadOnlyObservableCollection<T> Matches { get; }

        public void Postpone(DateTime? date = null, bool propagateToMatches = true)
        {
            IsPostponed = true;
            _postponedDate = date;
            RaisePropertyChanged(nameof(Date));

            if (propagateToMatches)
                Matches.Where(x => x.State is MatchState.None or MatchState.Postponed).ToList().ForEach(x => x.Postpone(date));
        }

        public void Schedule(DateTime date)
        {
            IsPostponed = false;
            _postponedDate = null;
            OriginDate = date;
        }

        public void ScheduleAll(DateTime date)
        {
            Schedule(date);

            Matches.Where(x => x.State is MatchState.None or MatchState.Postponed).ToList().ForEach(x =>
            {
                x.Reset();
                x.Schedule(date);
            });
        }

        public IEnumerable<Match> GetAllMatches() => _matches.AsEnumerable();

        public IEnumerable<T1> GetStages<T1>() where T1 : ICompetitionStage => [];

        public T AddMatch(IVirtualTeam homeTeam, IVirtualTeam awayTeam) => AddMatch(Date, homeTeam, awayTeam);

        public T AddMatch(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam) => AddMatch(Create(date, homeTeam, awayTeam));

        public virtual T AddMatch(T match)
        {
            if (_matches.Contains(match))
                throw new AlreadyExistsException(nameof(Matches), match);

            _matches.Add(match);

            return match;
        }

        Match IMatchesStage.AddMatch(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam) => AddMatch(date, homeTeam, awayTeam);

        Match IMatchesStage.AddMatch(IVirtualTeam homeTeam, IVirtualTeam awayTeam) => AddMatch(homeTeam, awayTeam);

        public bool RemoveMatch(Match item) => item is T t && _matches.Remove(t);

        protected abstract T Create(DateTime date, IVirtualTeam homeTeam, IVirtualTeam awayTeam);

        public abstract MatchFormat ProvideFormat();

        public abstract MatchRules ProvideRules();

        public abstract SchedulingParameters ProvideSchedulingParameters();

        public abstract IEnumerable<IVirtualTeam> ProvideTeams();

        public override int CompareTo(object? obj) => obj is MatchesStage<T> other ? OriginDate.CompareTo(other.OriginDate) : 1;

        public override string ToString() => Name;
    }
}
