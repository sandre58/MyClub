// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using MyClub.Scorer.Wpf.Services.Providers;
using MyClub.Scorer.Wpf.ViewModels.HomePage;
using MyClub.Scorer.Wpf.ViewModels.PastPositionsPage;
using MyClub.Scorer.Wpf.ViewModels.SchedulePage;
using MyNet.UI.Navigation;
using MyNet.Utilities.Suspending;
using MyNet.Wpf.Schedulers;

namespace MyClub.Scorer.Wpf.Services
{
    internal sealed class NavigationCommandsService : WorkspaceNavigationService, IDisposable
    {
        public const string SelectionParameterKey = "Selection";

        private readonly CompositeDisposable _disposables = [];
        private readonly Suspender _navigationSuspender = new();

        public NavigationCommandsService(ProjectInfoProvider projectInfoProvider)
            => projectInfoProvider.WhenProjectClosing(() =>
            {
                using (_navigationSuspender.Suspend())
                {
                    if (CurrentContext?.Page is not HomePageViewModel)
                        WpfScheduler.Current.Schedule(() => NavigateToHomePage());
                    ClearJournal();
                }
            });

        public void Dispose() => _disposables.Dispose();

        public static void NavigateToHomePage() => NavigationManager.NavigateTo<HomePageViewModel>();

        public static void NavigateToSchedulePage(IEnumerable<Guid>? selectedIds = null)
        {
            if (selectedIds is not null)
                NavigationManager.NavigateTo<SchedulePageViewModel>(selectedIds, SelectionParameterKey);
            else
                NavigationManager.NavigateTo<SchedulePageViewModel>();
        }

        public static void NavigateToPastPositionsPage(IEnumerable<Guid>? selectedIds = null)
        {
            if (selectedIds is not null)
                NavigationManager.NavigateTo<PastPositionsPageViewModel>(selectedIds, SelectionParameterKey);
            else
                NavigationManager.NavigateTo<PastPositionsPageViewModel>();
        }
    }
}
