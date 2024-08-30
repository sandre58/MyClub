// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Binding;
using MyClub.Scorer.Application.Services;
using MyClub.Scorer.Domain.CompetitionAggregate;
using MyClub.Scorer.Domain.Extensions;
using MyClub.Scorer.Domain.MatchAggregate;
using MyClub.Scorer.Domain.RankingAggregate;
using MyClub.Scorer.Domain.Scheduling;
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

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class LeagueViewModel : EntityViewModelBase<League>, ICompetitionViewModel, IMatchdayParent
    {
        private readonly LeagueService _leagueService;
        private readonly TeamsProvider _teamsProvider;
        private readonly UiObservableCollection<MatchdayViewModel> _matchdays = [];
        private readonly UiObservableCollection<MatchViewModel> _matches = [];
        private readonly RefreshDeferrer _refreshRankingsDeferrer = new();
        private readonly SingleTaskRunner _refreshRankingsRunner;
        private readonly Subject<bool> _rankingChangedSubject = new();

        public LeagueViewModel(League item,
                               IObservable<SchedulingParameters?> observableSchedulingParameters,
                               MatchdayPresentationService matchdayPresentationService,
                               MatchPresentationService matchPresentationService,
                               StadiumsProvider stadiumsProvider,
                               TeamsProvider teamsProvider,
                               LeagueService leagueService) : base(item)
        {
            _leagueService = leagueService;
            _teamsProvider = teamsProvider;
            Matchdays = new(_matchdays);
            SchedulingParameters = new SchedulingParametersViewModel(observableSchedulingParameters);
            _refreshRankingsRunner = new SingleTaskRunner(async x => await RefreshRankingsAsync(x).ConfigureAwait(false));

            _refreshRankingsDeferrer.Subscribe(this, () =>
            {
                _refreshRankingsRunner.Cancel();
                _refreshRankingsRunner.Run();
            }, 100);

            Ranking = new RankingViewModel(teamsProvider.Items, _matches);
            LiveRanking = new RankingViewModel(teamsProvider.Items, _matches);
            HomeRanking = new RankingViewModel(teamsProvider.Items, _matches);
            AwayRanking = new RankingViewModel(teamsProvider.Items, _matches);

            Disposables.AddRange(
            [
                item.WhenPropertyChanged(x => x.RankingRules, false).Subscribe(_ => _refreshRankingsDeferrer.AskRefresh()),
                item.Matchdays.ToObservableChangeSet(x => x.Id)
                              .Transform(x => new MatchdayViewModel(x, this, matchdayPresentationService, matchPresentationService, stadiumsProvider, teamsProvider))
                              .Bind(_matchdays)
                              .DisposeMany()
                              .Subscribe(_ => _refreshRankingsDeferrer.AskRefresh()),
                _matchdays.ToObservableChangeSet(x => x.Id)
                          .MergeManyEx(x => x.Matches.ToObservableChangeSet(x => x.Id), x => x.Id)
                          .Bind(_matches)
                          .Subscribe(),
                _matches.ToObservableChangeSet(x => x.Id).WhereReasonsAre(ChangeReason.Add, ChangeReason.Remove).Batch(100.Milliseconds()).Subscribe(_ => _refreshRankingsDeferrer.AskRefresh()),
                _matches.ToObservableChangeSet(x => x.Id).SubscribeMany(x => Observable.FromEventPattern<EventHandler, EventArgs>(y => x.ScoreChanged += y, y => x.ScoreChanged -= y).Subscribe(_ => _refreshRankingsDeferrer.AskRefresh())).Subscribe(),
            ]);
        }

        public SchedulingParametersViewModel SchedulingParameters { get; private set; }

        public ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        public MatchFormat MatchFormat => Item.MatchFormat;

        public RankingRules RankingRules => Item.RankingRules;

        public RankingViewModel Ranking { get; }

        public RankingViewModel LiveRanking { get; }

        public RankingViewModel HomeRanking { get; }
        public RankingViewModel AwayRanking { get; }

        public bool CanAutomaticReschedule() => Item.SchedulingParameters.CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Item.SchedulingParameters.CanAutomaticRescheduleVenue();

        public IEnumerable<ITeamViewModel> GetAvailableTeams() => _teamsProvider.Items;

        public IObservable<IChangeSet<MatchViewModel, Guid>> ProvideMatches() => _matches.ToObservableChangeSet(x => x.Id);

        public IObservable<IChangeSet<IMatchParent, Guid>> ProvideMatchParents() => _matchdays.ToObservableChangeSet(x => x.Id).Transform(x => (IMatchParent)x);

        public IDisposable WhenRankingChanged(Action action)
            => !_rankingChangedSubject.IsDisposed ? _rankingChangedSubject.Subscribe(_ => action()) : Disposable.Empty;

        public IDisposable DeferRefreshRankings() => _refreshRankingsDeferrer.Defer();

        private async Task RefreshRankingsAsync(CancellationToken cancellationToken)
            => await AppBusyManager.BackgroundAsync(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    using (LogManager.MeasureTime("Compute Rankings"))
                    {
                        LiveRanking.Update(_leagueService.GetRanking(true));

                        cancellationToken.ThrowIfCancellationRequested();

                        Ranking.Update(_leagueService.GetRanking());

                        cancellationToken.ThrowIfCancellationRequested();

                        HomeRanking.Update(_leagueService.GetHomeRanking());

                        cancellationToken.ThrowIfCancellationRequested();

                        AwayRanking.Update(_leagueService.GetAwayRanking());

                        cancellationToken.ThrowIfCancellationRequested();

                        _rankingChangedSubject.OnNext(true);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Nothing
                }
            }).ConfigureAwait(false);

        protected override void Cleanup()
        {
            base.Cleanup();
            Ranking.Dispose();
            HomeRanking.Dispose();
            AwayRanking.Dispose();
            SchedulingParameters.Dispose();
            _refreshRankingsDeferrer.Dispose();
            _refreshRankingsRunner.Dispose();
        }
    }
}
