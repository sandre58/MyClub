// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Subjects;
using MyNet.UI.Navigation.Models;
using MyNet.UI.ViewModels;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class CupPageViewModel : CompetitionPageViewModel<CupViewModel>, IItemViewModel<CupViewModel>
    {
        public CupPageViewModel(ProjectInfoProvider projectInfoProvider,
                                CompetitionsProvider competitionsProvider,
                                RoundPresentationService roundPresentationService,
                                MatchdayPresentationService matchdayPresentationService,
                                MatchPresentationService matchPresentationService,
                                HolidaysProvider holidaysProvider)
            : base(projectInfoProvider, competitionsProvider)
        {
            OverviewViewModel = new();
            RoundsViewModel = new(roundPresentationService, matchdayPresentationService, matchPresentationService, CompetitionChanged);
            MatchdaysViewModel = new(roundPresentationService, matchdayPresentationService, matchPresentationService, holidaysProvider, CompetitionChanged);

            AddSubWorkspaces(
            [
                OverviewViewModel,
                RoundsViewModel,
                MatchdaysViewModel,
                StadiumsViewModel,
                RulesViewModel,
            ]);
        }

        public CupPageOverviewViewModel OverviewViewModel { get; }

        public CupPageRoundsViewModel RoundsViewModel { get; }

        public CupPageMatchdaysViewModel MatchdaysViewModel { get; }

        CupViewModel? IItemViewModel<CupViewModel>.Item => Competition;

        Subject<CupViewModel?> IItemViewModel<CupViewModel>.ItemChanged => CompetitionChanged;

        public override void LoadParameters(INavigationParameters? parameters)
        {
            if (parameters is null) return;

            if (parameters.Get<RoundViewModel>(NavigationCommandsService.ItemParameterKey) is RoundViewModel item)
            {
                Item = item.Parent;
                RoundsViewModel.SelectRound(item);
                GoToTab(RoundsViewModel);
            }
            else
                base.LoadParameters(parameters);
        }

        protected override void Cleanup()
        {
            base.Cleanup();
            OverviewViewModel.Dispose();
            RoundsViewModel.Dispose();
            MatchdaysViewModel.Dispose();
        }
    }
}
