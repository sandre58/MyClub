// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Subjects;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels;
using MyClub.Teamup.Application.Services;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class LeaguePageViewModel : CompetitionPageViewModel<LeagueViewModel>, IItemViewModel<LeagueViewModel>
    {
        public LeaguePageViewModel(ProjectInfoProvider projectInfoProvider,
                                   CompetitionsProvider competitionsProvider,
                                   MatchdayPresentationService matchdayPresentationService,
                                   MatchPresentationService matchPresentationService,
                                   CompetitionService competitionService,
                                   HolidaysProvider holidaysProvider)
            : base(projectInfoProvider, competitionsProvider)
        {
            OverviewViewModel = new();
            PastPositionsViewModel = new(competitionService);
            RankingViewModel = new();
            MatchesViewModel = new(matchdayPresentationService, matchPresentationService, CompetitionChanged);
            MatchdaysViewModel = new(matchdayPresentationService, matchPresentationService, holidaysProvider, CompetitionChanged);
            TeamStatisticsViewModel = new();
            PlayerStatisticsViewModel = new();

            AddSubWorkspaces(
            [
                OverviewViewModel,
                RankingViewModel,
                PastPositionsViewModel,
                MatchesViewModel,
                MatchdaysViewModel,
                TeamStatisticsViewModel,
                PlayerStatisticsViewModel,
                StadiumsViewModel,
                RulesViewModel
            ]);
        }

        public LeaguePageOverviewViewModel OverviewViewModel { get; }

        public LeaguePageRankingViewModel RankingViewModel { get; }

        public LeaguePagePastPositionsViewModel PastPositionsViewModel { get; }

        public LeaguePageMatchesViewModel MatchesViewModel { get; }

        public LeaguePageMatchdaysViewModel MatchdaysViewModel { get; }

        public LeaguePageTeamStatisticsViewModel TeamStatisticsViewModel { get; }

        public LeaguePagePlayerStatisticsViewModel PlayerStatisticsViewModel { get; }

        LeagueViewModel? IItemViewModel<LeagueViewModel>.Item => Competition;

        Subject<LeagueViewModel?> IItemViewModel<LeagueViewModel>.ItemChanged => CompetitionChanged;

        public override void LoadParameters(INavigationParameters? parameters)
        {
            if (parameters is null) return;

            if (parameters.Get<MatchdayViewModel>(NavigationCommandsService.ItemParameterKey) is MatchdayViewModel item)
            {
                Item = item.Parent as LeagueViewModel;
                MatchesViewModel.SelectMatchday(item);
                GoToTab(MatchesViewModel);
            }
            else
                base.LoadParameters(parameters);
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            OverviewViewModel.Dispose();
            RankingViewModel.Dispose();
            PastPositionsViewModel.Dispose();
            MatchesViewModel.Dispose();
            MatchdaysViewModel.Dispose();
            TeamStatisticsViewModel.Dispose();
            PlayerStatisticsViewModel.Dispose();
        }
    }
}
