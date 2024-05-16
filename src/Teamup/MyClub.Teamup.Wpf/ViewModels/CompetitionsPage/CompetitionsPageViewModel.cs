// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MyNet.UI.ViewModels;
using MyClub.Teamup.Wpf.Extensions;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.CompetitionsPage.CompetitionsTab;
using MyClub.Teamup.Wpf.ViewModels.CompetitionsPage.OverviewTab;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.CompetitionsPage
{
    internal class CompetitionsPageViewModel : PageViewModel
    {
        public OverviewViewModel OverviewViewModel { get; }

        public CompetitionsViewModel CompetitionsViewModel { get; }

        public CompetitionsPageViewModel(
            ProjectInfoProvider projectInfoProvider,
            CompetitionsProvider competitionsProvider,
            FriendlyPresentationService friendlyPresentationService,
            LeaguePresentationService leaguePresentationService,
            CupPresentationService cupPresentationService,
            CompetitionPresentationService competitionPresentationService) : base(projectInfoProvider)
        {
            OverviewViewModel = new OverviewViewModel();
            CompetitionsViewModel = new CompetitionsViewModel(competitionsProvider, friendlyPresentationService, leaguePresentationService, cupPresentationService, competitionPresentationService);
            AddSubWorkspaces([OverviewViewModel, CompetitionsViewModel]);
        }

        protected override void ResetFromMainTeams(IEnumerable<Guid>? mainTeams)
        {
            base.ResetFromMainTeams(mainTeams);
            CompetitionsViewModel.ResetFiltersWithTeams(mainTeams, $"{nameof(CompetitionViewModel.Teams)}.{nameof(TeamViewModel.Id)}");
        }

        protected override void Cleanup()
        {
            CompetitionsViewModel.Dispose();
            OverviewViewModel.Dispose();
            base.Cleanup();
        }
    }
}
