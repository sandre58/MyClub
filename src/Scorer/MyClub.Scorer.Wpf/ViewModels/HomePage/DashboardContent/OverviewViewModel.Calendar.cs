// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using System;
using DynamicData;
using MyClub.Scorer.Wpf.Services;
using MyClub.Scorer.Wpf.Services.Providers;
using MyNet.Observable;
using MyNet.Observable.Attributes;
using MyNet.UI.Commands;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.Utilities;
using MyNet.Utilities.Localization;

namespace MyClub.Scorer.Wpf.ViewModels.HomePage.DashboardContent
{
    internal class OverviewCalendarViewModel : ListViewModel<IAppointment>
    {
        [UpdateOnTimeZoneChanged]
        public virtual DateOnly DisplayDate => GlobalizationService.Current.Date.ToDate();

        public ICommand NavigateToCalendarCommand { get; }

        public OverviewCalendarViewModel(MatchesProvider matchesProvider)
            : base(matchesProvider.Connect().Transform(x => (IAppointment)x)) => NavigateToCalendarCommand = CommandsManager.Create<DateTime?>(x => NavigationCommandsService.NavigateToSchedulePage(nameof(DisplayModeDay), x!.Value.ToDate()), x => x is not null);
    }
}
