// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Media;
using DynamicData;
using DynamicData.Binding;
using MyNet.UI.Services;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities;
using MyNet.DynamicData.Extensions;
using MyNet.Utilities.Logging;
using MyNet.Observable.Collections;
using MyNet.Observable.Collections.Filters;
using MyNet.Observable.Collections.Sorting;
using MyNet.UI.Threading;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using PropertyChanged;
using MyNet.UI.Collections;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class GroupViewModel : EntityViewModelBase<Group>, IMatchParent
    {
        private readonly TeamsProvider _teamsProvider;
        private readonly ExtendedCollection<MatchViewModel> _matches;
        private readonly UiObservableCollection<TeamViewModel> _teams = [];
        private readonly Subject<bool> _rankingRefreshedSuject = new();
        private readonly Subject<bool> _refreshRankingRequestedSubject = new();

        public GroupViewModel(Group item,
                              GroupStageViewModel parent,
                              TeamsProvider teamsProvider)
            : base(item)
        {
            _teamsProvider = teamsProvider;
            Parent = parent;
            Teams = new(_teams);
            Ranking = new RankingViewModel(Teams);
            HomeRanking = new RankingViewModel(Teams);
            AwayRanking = new RankingViewModel(Teams);

            _matches = new ExtendedCollection<MatchViewModel>(Scheduler.UI);
            _matches.SortingProperties.Add(new SortingProperty(nameof(MatchViewModel.Date)));
            _matches.Filters.Add(new CompositeFilter(new BooleanFilterViewModel(nameof(MatchViewModel.IsMyMatch)) { Value = true }));

            Disposables.AddRange(
            [
                _refreshRankingRequestedSubject.Throttle(50.Milliseconds()).Subscribe(async _ => await RefreshRankingsAsync().ConfigureAwait(false)),
                item.WhenPropertyChanged(x => x.Rules).Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true)),
                item.WhenPropertyChanged(x => x.Penalties).Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true)),
                item.Teams.ToObservableChangeSet().Transform(x => _teamsProvider.GetOrThrow(x.Id)).ObserveOn(Scheduler.UI).Bind(_teams).Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true)),
                parent.Matchdays.ToObservableChangeSet()
                          .MergeManyEx(x => x.AllMatches.ToObservableChangeSet())
                          .Filter(x => Teams.Any(y => x.Participate(y)))
                          .OnItemAdded(AddMatch)
                          .OnItemRemoved(RemoveMatch)
                          .Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true))
            ]);
        }

        public Color Color => Parent.Color;

        public GroupStageViewModel Parent { get; }

        IMatchParent IMatchParent.Parent => Parent;

        public string Name => Item.Name;

        public string ShortName => Item.ShortName;

        public CompetitionRules Rules => Item.Rules;

        public ReadOnlyObservableCollection<TeamViewModel> Teams { get; }

        public ReadOnlyObservableCollection<MatchViewModel> Matches => _matches.Items;

        public ReadOnlyObservableCollection<MatchViewModel> AllMatches => _matches.Source;

        public bool HasMatches => AllMatches.Any();

        public RankingViewModel Ranking { get; }

        public RankingViewModel HomeRanking { get; }

        public RankingViewModel AwayRanking { get; }

        public RankingRules RankingRules => Rules.CastIn<ChampionshipRules>().RankingRules;

        public IDictionary<TeamViewModel, int> Penalties => Item.Penalties?.ToDictionary(x => Teams.GetById(x.Key.Id), x => x.Value) ?? [];

        protected void AddMatch(MatchViewModel match)
        {
            _matches.Add(match);
            match.ScoreChanged += OnScoreChanged;
            RaisePropertyChanged(nameof(HasMatches));
        }

        protected void RemoveMatch(MatchViewModel match)
        {
            match.ScoreChanged -= OnScoreChanged;
            _matches.Remove(match);
            RaisePropertyChanged(nameof(HasMatches));
        }

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged(object? sender, EventArgs _) => _refreshRankingRequestedSubject.OnNext(true);

        private async Task RefreshRankingsAsync()
        {
            LogManager.Trace($"Refresh - League:{Item} [Rankings]");

            await AppBusyManager.BackgroundAsync(() =>
            {
                Ranking.Update(Item.GetRanking(), AllMatches);
                HomeRanking.Update(Item.GetHomeRanking(), AllMatches);
                AwayRanking.Update(Item.GetAwayRanking(), AllMatches);

                _rankingRefreshedSuject.OnNext(true);
            }).ConfigureAwait(false);
        }

        public IDisposable SubscribeOnRankingRefreshed(Action action)
            => !_rankingRefreshedSuject.IsDisposed ? _rankingRefreshedSuject.Subscribe(_ => action()) : Disposable.Empty;

        public IEnumerable<MatchViewModel> GetAllMatches() => AllMatches;

        public DateTime GetDefaultDateTime() => Parent.GetDefaultDateTime();

        public IEnumerable<TeamViewModel> GetAvailableTeams() => Parent.GetAvailableTeams();

        public bool CanCancelMatch() => Parent.CanCancelMatch();

        public bool CanEditMatchFormat() => Parent.CanEditMatchFormat();

        public bool CanEditPenaltyPoints() => Parent.CanEditPenaltyPoints();

        protected override void Cleanup()
        {
            base.Cleanup();
            Ranking.Dispose();
            HomeRanking.Dispose();
            AwayRanking.Dispose();
            _rankingRefreshedSuject.Dispose();
            _refreshRankingRequestedSubject.Dispose();
        }
    }
}
