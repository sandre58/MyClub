// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData.Binding;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.HomePage.DashboardContent;
using MyClub.Scorer.Wpf.ViewModels.HomePage.OpenAssistantContent;
using MyNet.UI.ViewModels.FileHistory;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage
{
    internal class HomePageViewModel : PageViewModel
    {
        public bool ProjectIsLoaded { get; private set; }

        public override bool CanDropScprojFiles => true;

        public OpenAssistantViewModel OpenAssistant { get; }

        public DashboardViewModel Dashboard { get; }

        public HomePageViewModel(ProjectCommandsService projectCommandsService,
                                 ProjectInfoProvider projectInfoProvider,
                                 MatchesProvider matchesProvider,
                                 TeamsProvider teamsProvider,
                                 StadiumsProvider stadiumsProvider,
                                 RecentFilesViewModel recentFilesViewModel)
        {
            OpenAssistant = new(recentFilesViewModel, projectCommandsService);
            Dashboard = new(projectInfoProvider, matchesProvider, teamsProvider, stadiumsProvider);
            Disposables.Add(projectInfoProvider.WhenPropertyChanged(x => x.IsLoaded).Subscribe(x => ProjectIsLoaded = x.Value));
        }

        protected override void Cleanup()
        {
            OpenAssistant.Dispose();
            Dashboard.Dispose();
            base.Cleanup();
        }
    }
}
