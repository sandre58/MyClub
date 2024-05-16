// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;

namespace MyClub.Scorer.Wpf.ViewModels.StadiumsPage
{
    internal class StadiumsPageViewModel : PageViewModel
    {
        public StadiumsViewModel StadiumsViewModel { get; }

        public StadiumsPageViewModel(StadiumsProvider stadiumsProvider,
                                     TeamsProvider teamsProvider,
                                     StadiumPresentationService stadiumPresentationService) => StadiumsViewModel = new StadiumsViewModel(stadiumsProvider, teamsProvider, stadiumPresentationService);

        protected override void Cleanup()
        {
            StadiumsViewModel.Dispose();
            base.Cleanup();
        }
    }
}
