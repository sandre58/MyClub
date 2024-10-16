// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Dtos;
using MyClub.Scorer.Domain.RankingAggregate;
using MyNet.Observable;
using MyNet.Observable.Deferrers;
using MyNet.UI.Collections;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Sequences;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class RankingViewModel : ObservableObject, IReadOnlyCollection<RankingRowViewModel>, INotifyCollectionChanged, IIdentifiable<Guid>
    {
        private readonly UiObservableCollection<RankingRowViewModel> _rows = [];
        private readonly IEnumerable<MatchViewModel> _matches;
        private readonly Subject<bool> _sortRowsSubject = new();

        public RankingViewModel(ReadOnlyObservableCollection<TeamViewModel> teams, IEnumerable<MatchViewModel> matches)
            : base()
        {
            UpdateRunner = new(x =>
            {
                var matches = _matches!.ToList();
                _rows.ToList().ForEach(y =>
                {
                    x.token?.ThrowIfCancellationRequested();
                    var rowFound = x.ranking.Rows?.Find(z => z.TeamId == y.Team.Id);

                    if (rowFound is not null)
                        y.Update(rowFound, rowFound.MatchIds?.Select(z => matches.GetByIdOrDefault(z)).NotNull().ToList() ?? []);
                    else
                        y.Reset();
                });

                x.token?.ThrowIfCancellationRequested();

                var teams = _rows.Select(x => x.Team).ToList();
                Rules = x.ranking.Rules;
                PenaltyPoints = x.ranking.PenaltyPoints?.ToDictionary(z => teams.GetById(z.Key), x => x.Value).AsReadOnly();
                Labels = x.ranking.Labels?.AsReadOnly();

                x.token?.ThrowIfCancellationRequested();
                _sortRowsSubject.OnNext(true);
            }, true);
            UpdateRunner.RegisterOnEnd(this, x => LogManager.Trace($"{GetType().Name} : Update ranking '{x.Id}' in {UpdateRunner.LastTimeElapsed.Milliseconds}ms"));

            Disposables.AddRange(
                [
                teams.ToObservableChangeSet().Transform(x => new RankingRowViewModel(this, x))
                                             .DisposeMany()
                                             .Bind(_rows)
                                             .Subscribe(),
                _rows.ToObservableChangeSet().SkipInitial()
                                             .AutoRefreshOnObservable(x => _sortRowsSubject)
                                             .Sort(SortExpressionComparer<RankingRowViewModel>.Ascending(x => x.Rank))
                                             .Subscribe(),
                _rows.ToObservableChangeSet().WhereReasonsAre(ListChangeReason.Add, ListChangeReason.Remove).Subscribe(_ => RaisePropertyChanged(nameof(Count)))
                                 ]);
            _matches = matches;

            _rows.CollectionChanged += new NotifyCollectionChangedEventHandler(HandleCollectionChanged);
        }

        public Guid Id { get; } = Guid.NewGuid();

        public ActionRunner<(RankingDto ranking, CancellationToken? token), RankingViewModel> UpdateRunner { get; }

        public int FormCount { get; set; } = 5;

        public int Count => _rows.Count;

        public RankingRules? Rules { get; private set; }

        public ReadOnlyDictionary<TeamViewModel, int>? PenaltyPoints { get; private set; }

        public ReadOnlyDictionary<AcceptableValueRange<int>, RankLabel>? Labels { get; private set; }

        public RankingRowViewModel? GetRow(TeamViewModel team) => _rows.FirstOrDefault(x => x.Team == team);

        public void Update(RankingDto ranking, CancellationToken? token = null) => UpdateRunner.Run((ranking, token), () => this);

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
