// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.RankingAggregate;
using MyNet.Observable;
using MyNet.Observable.Collections;
using MyNet.Utilities;
using MyNet.Utilities.Sequences;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class RankingViewModel : ObservableObject, IReadOnlyCollection<RankingRowViewModel>, INotifyCollectionChanged
    {
        private readonly ThreadSafeObservableCollection<RankingRowViewModel> _rows = [];
        private readonly IEnumerable<MatchViewModel> _matches;
        private readonly Subject<bool> _sortRowsSubject = new();

        public RankingViewModel(ReadOnlyObservableCollection<TeamViewModel> teams, IEnumerable<MatchViewModel> matches)
            : base()
        {
            Disposables.Add(teams.ToObservableChangeSet()
                                 .Transform(x => new RankingRowViewModel(this, x))
                                 .AutoRefreshOnObservable(x => _sortRowsSubject)
                                 .Sort(SortExpressionComparer<RankingRowViewModel>.Ascending(x => x.Rank))
                                 .DisposeMany()
                                 .Bind(_rows)
                                 .Subscribe(_ => RaisePropertyChanged(nameof(Count))));
            _matches = matches;

            _rows.CollectionChanged += new NotifyCollectionChangedEventHandler(HandleCollectionChanged);
        }

        public RankingRowViewModel? GetRow(TeamViewModel team) => _rows.FirstOrDefault(x => x.Team == team);

        public void Update(RankingDto ranking)
        {
            var matches = _matches.ToList();
            _rows.ToList().ForEach(x =>
            {
                var rowFound = ranking.Rows?.Find(y => y.TeamId == x.Team.Id);

                if (rowFound is not null)
                    x.Update(rowFound, rowFound.MatchIds?.Select(y => matches.GetByIdOrDefault(y)).NotNull().ToList() ?? []);
                else
                    x.Reset();
            });

            var teams = _rows.Select(x => x.Team).ToList();
            Rules = ranking.Rules;
            PenaltyPoints = ranking.PenaltyPoints?.ToDictionary(x => teams.GetById(x.Key), x => x.Value).AsReadOnly();
            Labels = ranking.Labels?.AsReadOnly();

            _sortRowsSubject.OnNext(true);
        }

        public int FormCount { get; set; } = 5;

        public int Count => _rows.Count;

        public RankingRules? Rules { get; private set; }

        public ReadOnlyDictionary<TeamViewModel, int>? PenaltyPoints { get; private set; }

        public ReadOnlyDictionary<AcceptableValueRange<int>, RankLabel>? Labels { get; private set; }

        #region INotifyCollectionChanged

        event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
        {
            add => CollectionChanged += value;
            remove => CollectionChanged -= value;
        }

        protected event NotifyCollectionChangedEventHandler? CollectionChanged;

        [SuppressPropertyChangedWarnings]
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args) => CollectionChanged?.Invoke(this, args);

        private void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => OnCollectionChanged(e);

        #endregion INotifyCollectionChanged

        public IEnumerator<RankingRowViewModel> GetEnumerator() => _rows.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            var str = new StringBuilder();

            this.ForEach((x, index) => str.AppendLine($"{index + 1} : {x.ToString()}"));

            return str.ToString();
        }

        protected override void Cleanup()
        {
            _rows.CollectionChanged -= new NotifyCollectionChangedEventHandler(HandleCollectionChanged);
            base.Cleanup();
        }
    }
}
