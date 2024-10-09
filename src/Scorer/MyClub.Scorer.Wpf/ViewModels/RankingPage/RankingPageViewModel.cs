// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MyClub.Domain.Enums;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyNet.UI.Commands;
using MyNet.Utilities;

namespace MyClub.Scorer.Wpf.ViewModels.RankingPage
{
    internal class RankingPageViewModel : PageViewModel
    {
        private readonly RankingListParameterProvider _rankingListParameterProvider = RankingListParameterProvider.Full;
        private CompositeDisposable? _disposables;

        public RankingPageViewModel(CompetitionInfoProvider competitionInfoProvider, MatchesProvider matchesProvider, LeaguePresentationService leaguePresentationService)
        {
            EditRulesCommand = CommandsManager.Create(async () => await leaguePresentationService.EditRankingRulesAsync().ConfigureAwait(false));

            Disposables.AddRange(
                [
                    matchesProvider.Connect()
                                   .AutoRefreshOnObservable(x => x.WhenPropertyChanged(y => y.State, false))
                                   .AutoRefreshOnObservable(_ => matchesProvider.LoadRunner.WhenEnd())
                                   .Subscribe(_ => LiveIsEnabled = !matchesProvider.LoadRunner.IsRunning && matchesProvider.Items.Any(x => x.State == MatchState.InProgress)),
                    competitionInfoProvider.UnloadRunner.WhenStart().Subscribe(_ =>
                    {
                        Ranking?.Dispose();
                        LiveRanking?.Dispose();
                        HomeRanking?.Dispose();
                        AwayRanking?.Dispose();

                        Ranking = null;
                        LiveRanking = null;
                        HomeRanking = null;
                        AwayRanking = null;
                        _disposables?.Dispose();
                    }),
                    competitionInfoProvider.LoadRunner.WhenEnd().Subscribe(x =>
                    {
                        if (x is LeagueViewModel leagueViewModel)
                        {
                            Ranking = new RankingListViewModel(leagueViewModel.Ranking, _rankingListParameterProvider);
                            LiveRanking = new RankingListViewModel(leagueViewModel.LiveRanking, _rankingListParameterProvider);
                            HomeRanking = new RankingListViewModel(leagueViewModel.HomeRanking, _rankingListParameterProvider);
                            AwayRanking = new RankingListViewModel(leagueViewModel.AwayRanking, _rankingListParameterProvider);

                            _disposables = new(leagueViewModel.SchedulingParameters.WhenPropertyChanged(x => x.UseHomeVenue).Subscribe(x => ShowHomeAwayRankings = x.Value));
                        }
                    })
               ]);
        }

        public bool LiveIsEnabled { get; private set; }

        public RankingListViewModel? Ranking { get; private set; }

        public RankingListViewModel? LiveRanking { get; private set; }

        public RankingListViewModel? HomeRanking { get; private set; }

        public RankingListViewModel? AwayRanking { get; private set; }

        public ICommand EditRulesCommand { get; private set; }

        public bool ShowHomeAwayRankings { get; set; }
    }
}
