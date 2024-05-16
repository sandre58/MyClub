// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Application.Deferrers;
using MyClub.Teamup.Wpf.Services;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.HomePage.DashboardContent;
using MyClub.Teamup.Wpf.ViewModels.HomePage.OpenAssistantContent;
using MyNet.UI.ViewModels.FileHistory;

namespace MyClub.Teamup.Wpf.ViewModels.HomePage
{
    internal class HomePageViewModel : PageViewModel
    {
        public override bool CanDropTmprojFiles => true;

        public DashboardViewModel Dashboard { get; }

        public OpenAssistantViewModel OpenAssistant { get; }

        public HomePageViewModel(ProjectCommandsService projectCommandsService,
                                 TrainingSessionPresentationService trainingSessionPresentationService,
                                 PlayerPresentationService playerPresentationService,
                                 ProjectInfoProvider projectInfoProvider,
                                 RecentFilesViewModel recentFilesViewModel,
                                 MainItemsProvider mainItemsProvider,
                                 TeamsProvider teamsProvider,
                                 PlayersProvider playersProvider,
                                 TrainingSessionsProvider trainingSessionsProvider,
                                 HolidaysProvider holidaysProvider,
                                 InjuriesStatisticsRefreshDeferrer injuriesStatisticsRefreshDeferrer,
                                 TrainingStatisticsRefreshDeferrer trainingStatisticsRefreshDeferrer) : base(projectInfoProvider)
        {
            OpenAssistant = new(recentFilesViewModel, projectCommandsService);
            Dashboard = new(trainingSessionPresentationService, playerPresentationService, projectInfoProvider, mainItemsProvider, teamsProvider, playersProvider, trainingSessionsProvider, holidaysProvider, injuriesStatisticsRefreshDeferrer, trainingStatisticsRefreshDeferrer);
        }

        protected override void Cleanup()
        {
            OpenAssistant.Dispose();
            Dashboard.Dispose();
            base.Cleanup();
        }
    }
}
