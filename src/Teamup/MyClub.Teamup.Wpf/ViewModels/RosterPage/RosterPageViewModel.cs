// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;

namespace MyClub.Teamup.Wpf.ViewModels.RosterPage
{
    internal class RosterPageViewModel : PageViewModel
    {
        public PlayersViewModel PlayersViewModel { get; }

        public RosterPageViewModel(ProjectInfoProvider projectInfoProvider,
                                   PlayersProvider playersProvider,
                                   PlayerPresentationService playerPresentationService) : base(projectInfoProvider) => PlayersViewModel = new PlayersViewModel(playersProvider, playerPresentationService);

        protected override void ResetFromMainTeams(IEnumerable<Guid>? mainTeams)
        {
            base.ResetFromMainTeams(mainTeams);
            PlayersViewModel.ResetFiltersWithTeams(mainTeams);
        }

        protected override void Cleanup()
        {
            PlayersViewModel.Dispose();
            base.Cleanup();
        }
    }
}
