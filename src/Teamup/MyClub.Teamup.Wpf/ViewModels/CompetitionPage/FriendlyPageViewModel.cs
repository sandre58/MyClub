// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System.Reactive.Subjects;
using MyNet.UI.ViewModels;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionPage
{
    internal class FriendlyPageViewModel : CompetitionPageViewModel<FriendlyViewModel>, IItemViewModel<FriendlyViewModel>
    {
        public FriendlyPageViewModel(ProjectInfoProvider projectInfoProvider, CompetitionsProvider competitionsProvider, MatchPresentationService matchPresentationService)
            : base(projectInfoProvider, competitionsProvider)
        {
            OverviewViewModel = new();
            MatchesViewModel = new(matchPresentationService, CompetitionChanged);

            AddSubWorkspaces(
            [
                OverviewViewModel,
                MatchesViewModel,
                StadiumsViewModel,
                RulesViewModel,
            ]);
        }

        public FriendlyPageOverviewViewModel OverviewViewModel { get; }

        public FriendlyPageMatchesViewModel MatchesViewModel { get; }

        FriendlyViewModel? IItemViewModel<FriendlyViewModel>.Item => Competition;

        Subject<FriendlyViewModel?> IItemViewModel<FriendlyViewModel>.ItemChanged => CompetitionChanged;

        protected override void Cleanup()
        {
            base.Cleanup();
            OverviewViewModel.Dispose();
            MatchesViewModel.Dispose();
        }
    }
}
