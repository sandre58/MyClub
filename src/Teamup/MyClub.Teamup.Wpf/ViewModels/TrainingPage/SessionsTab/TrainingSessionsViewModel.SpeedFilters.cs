// Copyright (c) Stéphane ANDRE. All Right Reserved.
// See the LICENSE file in the project root for more information.

using MyNet.UI.ViewModels.List.Filtering;
using MyNet.UI.ViewModels.List.Filtering.Filters;
using MyNet.Utilities.Comparaison;
using MyClub.Teamup.Wpf.Filters;
using MyClub.Teamup.Wpf.Services.Providers;
using MyClub.Teamup.Wpf.ViewModels.Entities;

namespace MyClub.Teamup.Wpf.ViewModels.TrainingPage.SessionsTab
{
    internal class TrainingSessionsSpeedFiltersViewModel : SpeedFiltersViewModel
    {
        public TrainingSessionsSpeedFiltersViewModel(HolidaysProvider holidaysProvider)
        {
            IsInHolidaysFilter = new IsInHolidaysFilterViewModel(nameof(TrainingSessionViewModel.StartDate), holidaysProvider);
            AddRange([TeamsFilter, ThemeFilter, DateFilter, IsCancelledFilter, IsInHolidaysFilter]);
        }

        public StringFilterViewModel ThemeFilter { get; } = new(nameof(TrainingSessionViewModel.Theme));

        public DateFilterViewModel DateFilter { get; } = new(nameof(TrainingSessionViewModel.StartDate), ComplexComparableOperator.IsBetween, null, null);

        public BooleanFilterViewModel IsCancelledFilter { get; } = new(nameof(TrainingSessionViewModel.IsCancelled)) { AllowNullValue = true };

        public IsInHolidaysFilterViewModel IsInHolidaysFilter { get; }

        public TeamsFilterViewModel TeamsFilter { get; } = new TeamsFilterViewModel($"{nameof(TrainingSessionViewModel.Teams)}.{nameof(TeamViewModel.Id)}", nameof(TeamViewModel.Id)) { ShowExternalPlayers = false, ShowLogicalOperator = true };
    }
}
