// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;
using MyNet.UI.ViewModels;
using MyNet.UI.ViewModels.Display;
using MyNet.UI.ViewModels.List;
using MyNet.UI.ViewModels.List.Filtering;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab
{
    internal class TrainingSessionsListParametersProvider(HolidaysProvider holidaysProvider) : ListParametersProvider(nameof(TrainingSessionViewModel.StartDate))
    {
        private readonly HolidaysProvider _holidaysProvider = holidaysProvider;

        public override IFiltersViewModel ProvideFilters() => new TrainingSessionsSpeedFiltersViewModel(_holidaysProvider);

        public override IDisplayViewModel ProvideDisplay() => new DisplayViewModel().AddMode<DisplayModeMonth>(true).AddMode<DisplayModeGrid>().AddMode<DisplayModeList>();
    }
}
