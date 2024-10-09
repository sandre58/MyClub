// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;

namespace MyClub.Scorer.Wpf.ViewModels.TeamsPage
{
    internal class TeamsPageViewModel : PageViewModel
    {
        public TeamsViewModel TeamsViewModel { get; }

        public TeamsPageViewModel(TeamsProvider teamsProvider, TeamPresentationService teamPresentationService) => TeamsViewModel = new TeamsViewModel(teamsProvider, teamPresentationService);

        protected override void Cleanup()
        {
            TeamsViewModel.Dispose();
            base.Cleanup();
        }
    }
}
