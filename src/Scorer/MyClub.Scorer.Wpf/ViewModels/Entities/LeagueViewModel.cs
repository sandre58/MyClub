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
using MyClub.Scorer.Domain.Scheduling;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities.Interfaces;
using MyNet.DynamicData.Extensions;
using MyNet.Observable.Deferrers;
using MyNet.UI.Services;
using MyNet.UI.Threading;
using MyNet.Utilities;
using MyNet.Utilities.Logging;

namespace MyClub.Scorer.Wpf.ViewModels.Entities
{
    internal class LeagueViewModel : EntityViewModelBase<League>, ICompetitionViewModel, IMatchdaysStageViewModel
    {
        private readonly LeagueService _leagueService;
        private readonly TeamsProvider _teamsProvider;
        private readonly ExtendedObservableCollection<MatchdayViewModel> _matchdays = [];
        private readonly ExtendedObservableCollection<MatchViewModel> _matches = [];
        private readonly SingleTaskDeferrer _refreshRankingsDeferrer;
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
            Matches = new(_matches);
            SchedulingParameters = new SchedulingParametersViewModel(observableSchedulingParameters);
            _refreshRankingsDeferrer = new(async x => await RefreshRankingsAsync(x).ConfigureAwait(false), throttle: 100);

            Ranking = new RankingViewModel(teamsProvider.Items, _matches);
            LiveRanking = new RankingViewModel(teamsProvider.Items, _matches);
            HomeRanking = new RankingViewModel(teamsProvider.Items, _matches);
            AwayRanking = new RankingViewModel(teamsProvider.Items, _matches);

            Disposables.AddRange(
            [
                item.WhenPropertyChanged(x => x.RankingRules, false).Subscribe(_ => _refreshRankingsDeferrer.AskRefresh()),
                item.Matchdays.ToObservableChangeSet()
                              .Transform(x => new MatchdayViewModel(x, this, matchdayPresentationService, matchPresentationService, stadiumsProvider, teamsProvider))
                              .ObserveOn(Scheduler.GetUIOrCurrent())
                              .Bind(_matchdays)
                              .DisposeMany()
                              .Subscribe(),
                _matchdays.ToObservableChangeSet()
                          .MergeManyEx(x => x.Matches.ToObservableChangeSet())
                          .ObserveOn(Scheduler.GetUIOrCurrent())
                          .Bind(_matches)
                          .Subscribe(),
                _matches.ToObservableChangeSet().WhereReasonsAre(ListChangeReason.Add, ListChangeReason.Remove, ListChangeReason.RemoveRange, ListChangeReason.AddRange, ListChangeReason.Clear).Subscribe(_ => _refreshRankingsDeferrer.AskRefresh()),
                _matches.ToObservableChangeSet().SubscribeMany(x => Observable.FromEventPattern<EventHandler, EventArgs>(y => x.ScoreChanged += y, y => x.ScoreChanged -= y).Subscribe(_ => _refreshRankingsDeferrer.AskRefresh())).Subscribe(),
            ]);
        }

        public SchedulingParametersViewModel SchedulingParameters { get; private set; }

        public ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        public RankingViewModel Ranking { get; }

        public RankingViewModel LiveRanking { get; }

        public RankingViewModel HomeRanking { get; }

        public RankingViewModel AwayRanking { get; }

        public ReadOnlyObservableCollection<MatchViewModel> Matches { get; }

        public string Name => string.Empty;

        public string ShortName => string.Empty;

        public DateOnly ProvideStartDate() => Item.SchedulingParameters.StartDate;

        public DateOnly ProvideEndDate() => Item.SchedulingParameters.EndDate;

        public TimeOnly ProvideStartTime() => Item.SchedulingParameters.StartTime;

        public bool CanAutomaticReschedule() => Item.SchedulingParameters.CanAutomaticReschedule();

        public bool CanAutomaticRescheduleVenue() => Item.SchedulingParameters.CanAutomaticRescheduleVenue();

        public bool UseHomeVenue() => Item.SchedulingParameters.UseHomeVenue;

        public IEnumerable<IVirtualTeamViewModel> GetAvailableTeams() => _teamsProvider.Items;

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
                        LiveRanking.Update(_leagueService.GetRanking(true), cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

                        Ranking.Update(_leagueService.GetRanking(), cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

                        HomeRanking.Update(_leagueService.GetHomeRanking(), cancellationToken);

                        cancellationToken.ThrowIfCancellationRequested();

                        AwayRanking.Update(_leagueService.GetAwayRanking(), cancellationToken);

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
        }
    }
}
