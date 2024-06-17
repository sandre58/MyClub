// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Deferrers;
using MyNet.UI.Collections;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using MyNet.Utilities.Threading;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class LeagueViewModel : EntityViewModelBase<League>, ICompetitionViewModel, IMatchdayParent
    {
        private readonly LeagueService _leagueService;
        private readonly UiObservableCollection<MatchdayViewModel> _matchdays = [];
        private readonly UiObservableCollection<MatchViewModel> _matches = [];
        private readonly Subject<bool> _rankingChangedSubject = new();
        private readonly RefreshDeferrer _refreshRankingsDeferrer = new();
        private readonly SingleTaskRunner _refreshRankingsRunner;

        public LeagueViewModel(League item,
                               MatchdayPresentationService matchdayPresentationService,
                               MatchPresentationService matchPresentationService,
                               StadiumsProvider stadiumsProvider,
                               TeamsProvider teamsProvider,
                               LeagueService leagueService) : base(item)
        {
            _leagueService = leagueService;
            _refreshRankingsRunner = new SingleTaskRunner(async x => await RefreshRankingsAsync(x).ConfigureAwait(false));
            _refreshRankingsDeferrer.Subscribe(() => _refreshRankingsRunner.Run(), 80);
            Matchdays = new(_matchdays);

            Ranking = new RankingViewModel(teamsProvider.Items, _matches);
            LiveRanking = new RankingViewModel(teamsProvider.Items, _matches);
            HomeRanking = new RankingViewModel(teamsProvider.Items, _matches);
            AwayRanking = new RankingViewModel(teamsProvider.Items, _matches);

            Disposables.AddRange(
            [
                item.WhenPropertyChanged(x => x.RankingRules).Subscribe(_ => _refreshRankingsDeferrer.AskRefresh()),
                item.Teams.ToObservableChangeSet().SkipInitial().Subscribe(_ => _refreshRankingsDeferrer.AskRefresh()),
                item.Matchdays.ToObservableChangeSet(x => x.Id)
                              .Transform(x => new MatchdayViewModel(x, this, matchdayPresentationService, matchPresentationService, stadiumsProvider, teamsProvider))
                              .Bind(_matchdays)
                              .DisposeMany()
                              .Subscribe(),
                _matchdays.ToObservableChangeSet(x => x.Id)
                          .MergeManyEx(x => x.Matches.ToObservableChangeSet(x => x.Id), x => x.Id)
                          .Bind(_matches)
                          .Subscribe(),
                _matches.ToObservableChangeSet(x => x.Id)
                        .OnItemAdded(x => x.ScoreChanged += OnScoreChanged)
                        .OnItemRemoved(x => x.ScoreChanged -= OnScoreChanged)
                        .SkipInitial()
                        .Subscribe(x => x.Any(y => y.Current.HasResult).IfTrue(_refreshRankingsDeferrer.AskRefresh))
            ]);
        }

        public ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        public MatchFormat MatchFormat => Item.MatchFormat;

        public RankingRules RankingRules => Item.RankingRules;

        public RankingViewModel Ranking { get; }

        public RankingViewModel LiveRanking { get; }

        public RankingViewModel HomeRanking { get; }

        public RankingViewModel AwayRanking { get; }

        public int GetCountTeams() => Item.Teams.Count;

        public IObservable<IChangeSet<MatchViewModel, Guid>> ProvideMatches() => _matches.ToObservableChangeSet(x => x.Id);

        public IObservable<IChangeSet<IMatchParent, Guid>> ProvideMatchParents() => _matchdays.ToObservableChangeSet(x => x.Id).Transform(x => (IMatchParent)x);

        public IDisposable WhenRankingChanged(Action action)
            => !_rankingChangedSubject.IsDisposed ? _rankingChangedSubject.Subscribe(_ => action()) : Disposable.Empty;

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged(object? sender, EventArgs _) => _refreshRankingsDeferrer.AskRefresh();

        private async Task RefreshRankingsAsync(CancellationToken cancellationToken)
        {
            LogManager.Trace($"Compute Rankings");

            await AppBusyManager.BackgroundAsync(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                LiveRanking.Update(_leagueService.GetRanking(true));

                cancellationToken.ThrowIfCancellationRequested();

                Ranking.Update(_leagueService.GetRanking());

                cancellationToken.ThrowIfCancellationRequested();

                HomeRanking.Update(_leagueService.GetHomeRanking());

                cancellationToken.ThrowIfCancellationRequested();

                AwayRanking.Update(_leagueService.GetAwayRanking());

                cancellationToken.ThrowIfCancellationRequested();

                _rankingChangedSubject.OnNext(true);
            }).ConfigureAwait(false);
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            Ranking.Dispose();
            HomeRanking.Dispose();
            AwayRanking.Dispose();
            _refreshRankingsDeferrer.Dispose();
            _refreshRankingsRunner.Dispose();
        }
    }
}
