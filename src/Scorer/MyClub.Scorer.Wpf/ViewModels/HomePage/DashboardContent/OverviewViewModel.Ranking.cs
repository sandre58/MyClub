// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Concurrency;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.Entities;
using MyClub.Scorer.Wpf.ViewModels.RankingPage;
using MyNet.Observable;
using MyNet.UI.Commands;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class OverviewRankingViewModel : ObservableObject
    {
        private readonly CompetitionInfoProvider _competitionInfoProvider;
        private readonly RankingListParameterProvider _rankingListParameterProvider = RankingListParameterProvider.Compact;

        public OverviewRankingViewModel(CompetitionInfoProvider competitionInfoProvider)
            : base()
        {
            _competitionInfoProvider = competitionInfoProvider;
            competitionInfoProvider.LoadRunner.RegisterOnEnd(this, x => MyNet.UI.Threading.Scheduler.GetUIOrCurrent().Schedule(() =>
                                 {
                                     if (x is LeagueViewModel leagueViewModel)
                                     {
                                         Ranking = new RankingListViewModel(leagueViewModel.Ranking, _rankingListParameterProvider);
                                         LiveRanking = new RankingListViewModel(leagueViewModel.LiveRanking, _rankingListParameterProvider);
                                     }
                                 }));

            competitionInfoProvider.UnloadRunner.RegisterOnStart(this, () =>
            {
                Ranking?.Dispose();
                LiveRanking?.Dispose();
                Ranking = null;
                LiveRanking = null;
            });

            NavigateToRankingCommand = CommandsManager.Create(() => NavigationCommandsService.NavigateToRankingPage());
        }

        public bool ShowLive { get; set; }

        public RankingListViewModel? Ranking { get; private set; }

        public RankingListViewModel? LiveRanking { get; private set; }

        public ICommand NavigateToRankingCommand { get; private set; }

        protected override void Cleanup()
        {
            _competitionInfoProvider.LoadRunner.Unregister(this);
            _competitionInfoProvider.UnloadRunner.Unregister(this);
            base.Cleanup();
        }
    }
}
