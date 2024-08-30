// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.Observable;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.List;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class OverviewCalendarViewModel : ListViewModel<IAppointment>
    {
        //[UpdateOnTimeZoneChanged]
        public DateTime DisplayDate { get ; set; } = DateTime.UtcNow;

        public ICommand NavigateToCalendarCommand { get; }

        public OverviewCalendarViewModel(MatchesProvider matchesProvider)
            : base(matchesProvider.Connect().Transform(x => (IAppointment)x)) => NavigateToCalendarCommand = CommandsManager.Create(() => NavigationCommandsService.NavigateToSchedulePage());
    }
}
