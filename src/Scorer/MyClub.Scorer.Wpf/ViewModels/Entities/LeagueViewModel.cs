// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
using MyNet.Observable.Collections;
using MyNet.UI.Services;
using MyNet.Utilities;
using MyNet.Utilities.Logging;
using PropertyChanged;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class LeagueViewModel : EntityViewModelBase<League>, ICompetitionViewModel, IMatchdayParent
    {
        private readonly LeagueService _leagueService;
        private readonly ThreadSafeObservableCollection<MatchdayViewModel> _matchdays = [];
        private readonly Subject<bool> _rankingRefreshedSuject = new();
        private readonly Subject<bool> _refreshRankingRequestedSubject = new();

        public LeagueViewModel(League item,
                               MatchdayPresentationService matchdayPresentationService,
                               MatchPresentationService matchPresentationService,
                               StadiumsProvider stadiumsProvider,
                               TeamsProvider teamsProvider,
                               LeagueService leagueService) : base(item)
        {
            _leagueService = leagueService;
            Matchdays = new(_matchdays);
            Ranking = new RankingViewModel(teamsProvider.Items, _matchdays.SelectMany(x => x.Matches));
            LiveRanking = new RankingViewModel(teamsProvider.Items, _matchdays.SelectMany(x => x.Matches));
            HomeRanking = new RankingViewModel(teamsProvider.Items, _matchdays.SelectMany(x => x.Matches));
            AwayRanking = new RankingViewModel(teamsProvider.Items, _matchdays.SelectMany(x => x.Matches));

            Disposables.AddRange(
            [
                _refreshRankingRequestedSubject.Throttle(80.Milliseconds()).Subscribe(async _ => await RefreshRankingsAsync().ConfigureAwait(false)),
                item.WhenPropertyChanged(x => x.RankingRules).Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true)),
                item.Teams.ToObservableChangeSet().Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true)),
                item.Matchdays.ToObservableChangeSet(x => x.Id)
                              .Transform(x => new MatchdayViewModel(x, this, matchdayPresentationService, matchPresentationService, stadiumsProvider, teamsProvider))
                              .Bind(_matchdays)
                              .DisposeMany()
                              .Subscribe(),
                _matchdays.ToObservableChangeSet()
                              .MergeManyEx(x => x.Matches.ToObservableChangeSet())
                              .OnItemAdded(x => x.ScoreChanged += OnScoreChanged)
                              .OnItemRemoved(x => x.ScoreChanged += OnScoreChanged)
                              .Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true))
            ]);
        }

        public ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        public MatchFormat MatchFormat => Item.MatchFormat;

        public RankingRules RankingRules => Item.RankingRules;

        public RankingViewModel Ranking { get; }

        public RankingViewModel LiveRanking { get; }

        public RankingViewModel HomeRanking { get; }

        public RankingViewModel AwayRanking { get; }

        public IObservable<IChangeSet<MatchViewModel, Guid>> ProvideMatches()
            => _matchdays.ToObservableChangeSet(x => x.Id).MergeManyEx(x => x.Matches.ToObservableChangeSet(x => x.Id), x => x.Id);

        public IObservable<IChangeSet<IMatchParent, Guid>> ProvideMatchParents() => _matchdays.ToObservableChangeSet(x => x.Id).Transform(x => (IMatchParent)x);

        public IDisposable SubscribeOnRankingRefreshed(Action action)
            => !_rankingRefreshedSuject.IsDisposed ? _rankingRefreshedSuject.Subscribe(_ => action()) : Disposable.Empty;

        [SuppressPropertyChangedWarnings]
        private void OnScoreChanged(object? sender, EventArgs _) => _refreshRankingRequestedSubject.OnNext(true);

        private async Task RefreshRankingsAsync()
        {
            LogManager.Trace($"Compute Rankings");

            await AppBusyManager.BackgroundAsync(() =>
            {
                UpdateRankings();

                _rankingRefreshedSuject.OnNext(true);
            }).ConfigureAwait(false);
        }

        private void UpdateRankings()
        {
            LiveRanking.Update(_leagueService.GetRanking(true));
            Ranking.Update(_leagueService.GetRanking());
            HomeRanking.Update(_leagueService.GetHomeRanking());
            AwayRanking.Update(_leagueService.GetAwayRanking());
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            Ranking.Dispose();
            HomeRanking.Dispose();
            AwayRanking.Dispose();
        }
    }
}
