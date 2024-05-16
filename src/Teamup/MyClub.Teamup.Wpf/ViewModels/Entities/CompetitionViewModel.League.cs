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
using MyNet.UI.Commands;
using MyNet.UI.Services;
using MyNet.Wpf.Helpers;
using MyNet.Utilities;
using MyNet.DynamicData.Extensions;
using MyNet.Utilities.Logging;
using MyNet.Observable.Collections;
using MyClub.Teamup.Domain.CompetitionAggregate;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities.Interfaces;
using PropertyChanged;

namespace MyClub.Teamup.Wpf.ViewModels.Entities
{
    internal class LeagueViewModel : CompetitionViewModel, IMatchdayParent
    {
        private readonly LeaguePresentationService _leaguePresentationService;
        private readonly MatchdayPresentationService _matchdayPresentationService;
        private readonly ThreadSafeObservableCollection<MatchdayViewModel> _matchdays = [];
        private readonly Subject<bool> _rankingRefreshedSuject = new();
        private readonly Subject<bool> _refreshRankingRequestedSubject = new();

        public LeagueViewModel(LeagueSeason item,
                               LeaguePresentationService leaguePresentationService,
                               MatchdayPresentationService matchdayPresentationService,
                               MatchPresentationService matchPresentationService,
                               TeamsProvider teamsProvider,
                               StadiumsProvider stadiumsProvider)
            : base(item, teamsProvider)
        {
            _leaguePresentationService = leaguePresentationService;
            _matchdayPresentationService = matchdayPresentationService;
            Ranking = new RankingViewModel(Teams);
            HomeRanking = new RankingViewModel(Teams);
            AwayRanking = new RankingViewModel(Teams);
            Matchdays = new(_matchdays);

            AddMatchdaysCommand = CommandsManager.Create(async () => await AddMatchdaysAsync().ConfigureAwait(false));

            Disposables.AddRange(
            [
                _refreshRankingRequestedSubject.Throttle(50.Milliseconds()).Subscribe(async _ => await RefreshRankingsAsync().ConfigureAwait(false)),
                item.WhenPropertyChanged(x => x.Rules).Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true)),
                item.WhenPropertyChanged(x => x.Penalties).Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true)),
                item.Teams.ToObservableChangeSet().Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true)),
                item.Matchdays.ToObservableChangeSet(x => x.Id)
                              .Transform(x => new MatchdayViewModel(x, this, matchdayPresentationService, matchPresentationService, stadiumsProvider))
                              .BindItems(_matchdays)
                              .DisposeMany()
                              .Subscribe(),
                _matchdays.ToObservableChangeSet()
                          .MergeManyEx(x => x.AllMatches.ToObservableChangeSet())
                          .OnItemAdded(AddMatch)
                          .OnItemRemoved(RemoveMatch)
                          .Subscribe(_ => _refreshRankingRequestedSubject.OnNext(true))
            ]);
        }

        public override Color Color => WpfHelper.GetResource<Color>("Teamup.Colors.Competition.League");

        [SuppressPropertyChangedWarnings]
        protected override void OnScoreChanged(MatchViewModel? matchViewModel) => _refreshRankingRequestedSubject.OnNext(true);

        public override CompetitionType Type => CompetitionType.League;

        public RankingViewModel Ranking { get; }

        public RankingViewModel HomeRanking { get; }

        public RankingViewModel AwayRanking { get; }

        public RankingRules RankingRules => Rules.CastIn<LeagueRules>().RankingRules;

        public IDictionary<TeamViewModel, int> Penalties => Item.CastIn<LeagueSeason>().Penalties?.ToDictionary(x => Teams.GetById(x.Key.Id), x => x.Value) ?? [];

        public ReadOnlyObservableCollection<MatchdayViewModel> Matchdays { get; }

        public ICommand AddMatchdaysCommand { get; }

        public override async Task EditAsync() => await _leaguePresentationService.EditAsync(this).ConfigureAwait(false);

        public override async Task DuplicateAsync() => await _leaguePresentationService.DuplicateAsync(this).ConfigureAwait(false);

        public override async Task RemoveAsync() => await _leaguePresentationService.RemoveAsync([this]).ConfigureAwait(false);

        public async Task AddMatchdaysAsync() => await _matchdayPresentationService.AddMultipleAsync(this, [DateTime.Today]).ConfigureAwait(false);

        private async Task RefreshRankingsAsync()
        {
            LogManager.Trace($"Refresh - League:{Item} [Rankings]");

            await AppBusyManager.BackgroundAsync(() =>
            {
                Ranking.Update(Item.CastIn<LeagueSeason>().GetRanking(), AllMatches);
                HomeRanking.Update(Item.CastIn<LeagueSeason>().GetHomeRanking(), AllMatches);
                AwayRanking.Update(Item.CastIn<LeagueSeason>().GetAwayRanking(), AllMatches);

                _rankingRefreshedSuject.OnNext(true);
            }).ConfigureAwait(false);
        }

        public IDisposable SubscribeOnRankingRefreshed(Action action)
            => !_rankingRefreshedSuject.IsDisposed ? _rankingRefreshedSuject.Subscribe(_ => action()) : Disposable.Empty;

        protected override void Cleanup()
        {
            base.Cleanup();
            Ranking.Dispose();
            HomeRanking.Dispose();
            AwayRanking.Dispose();
            _rankingRefreshedSuject.Dispose();
            _refreshRankingRequestedSubject.Dispose();
        }

        public override bool CanCancelMatch() => false;

        public override bool CanEditMatchFormat() => false;

        public override bool CanEditPenaltyPoints() => true;

        public IEnumerable<IMatchdayViewModel> GetAllMatchdays() => Matchdays;
    }
}
